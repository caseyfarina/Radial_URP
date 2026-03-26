using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic; // For List

/// <summary>
/// Manages monitor detection and window movement between displays
/// </summary>
public class MonitorManager : MonoBehaviour
{
    [Header("Monitor Switching Settings")]
    [Tooltip("Key to move to previous monitor")]
    [SerializeField] private KeyCode previousMonitorKey = KeyCode.LeftBracket;

    [Tooltip("Key to move to next monitor")]
    [SerializeField] private KeyCode nextMonitorKey = KeyCode.RightBracket;

    [Tooltip("Show debug messages in the console")]
    [SerializeField] private bool debugMode = true;

    [Tooltip("Preserve aspect ratio when changing monitor")]
    [SerializeField] private bool preserveAspectRatio = true;

    [Header("Optional Settings")]
    [Tooltip("Set window to fullscreen when switching monitors")]
    [SerializeField] private bool setFullscreenOnSwitch = false;

    [Tooltip("Set window to borderless when switching monitors")]
    [SerializeField] private bool setBorderlessOnSwitch = true;

    [Header("Startup Settings")]
    [Tooltip("Initialize window position on startup")]
    [SerializeField] private bool initializeOnStartup = true;

    [Tooltip("Monitor index to use on startup (0 is primary monitor)")]
    [SerializeField, Range(0, 8)] private int startupMonitorIndex = 0;

    [Tooltip("Set fullscreen on startup")]
    [SerializeField] private bool setFullscreenOnStartup = false;

    [Tooltip("Set borderless window on startup")]
    [SerializeField] private bool setBorderlessOnStartup = false;

    [Header("Events")]
    [Tooltip("Event triggered when monitor changes")]
    [SerializeField] private UnityEngine.Events.UnityEvent<int> onMonitorChanged;

    // Store current monitor index
    private int currentMonitorIndex = 0;
    private int monitorCount = 1;

    // Windows API imports for monitor management
#if UNITY_STANDALONE_WIN
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public int Width => right - left;
        public int Height => bottom - top;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(System.IntPtr hdc, System.IntPtr lprcClip, MonitorEnumProc lpfnEnum, System.IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(System.IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern System.IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int GetSystemMetrics(int nIndex);

    // Monitor info structure
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        public void Init()
        {
            cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));
        }
    }

    // Monitor enumeration callback
    private delegate bool MonitorEnumProc(System.IntPtr hMonitor, System.IntPtr hdcMonitor, ref RECT lprcMonitor, System.IntPtr dwData);

    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const int SM_CMONITORS = 80;

    // List to store monitor info
    private System.Collections.Generic.List<MONITORINFO> monitors = new System.Collections.Generic.List<MONITORINFO>();

    [DllImport("user32.dll")]
    static extern System.IntPtr SetWindowLong(System.IntPtr hWnd, int nIndex, System.IntPtr dwNewLong);

    [DllImport("user32.dll")]
    static extern System.IntPtr GetWindowLong(System.IntPtr hWnd, int nIndex);

    private const int GWL_STYLE = -16;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;

    private static bool EnumMonitorsProc(System.IntPtr hMonitor, System.IntPtr hdcMonitor, ref RECT lprcMonitor, System.IntPtr dwData)
    {
        MONITORINFO mi = new MONITORINFO();
        mi.Init();

        if (GetMonitorInfo(hMonitor, ref mi))
        {
            // Convert to class instance for callback
            GCHandle gch = GCHandle.FromIntPtr(dwData);
            MonitorManager manager = (MonitorManager)gch.Target;

            if (manager != null)
            {
                manager.monitors.Add(mi);
                manager.DebugLog($"Found monitor: {mi.rcMonitor.left},{mi.rcMonitor.top} - {mi.rcMonitor.right}x{mi.rcMonitor.bottom}");
            }
        }

        return true;
    }
#endif

    private void Awake()
    {
        DetectMonitors();
    }

    private void Update()
    {
        if (Input.GetKeyDown(previousMonitorKey))
        {
            SwitchToPreviousMonitor();
        }
        else if (Input.GetKeyDown(nextMonitorKey))
        {
            SwitchToNextMonitor();
        }
    }

    public void DetectMonitors()
    {
#if UNITY_STANDALONE_WIN
        monitors.Clear();

        // Get monitor count
        monitorCount = GetSystemMetrics(SM_CMONITORS);
        DebugLog($"Detected {monitorCount} monitors");

        // Pass this instance to the callback
        GCHandle gch = GCHandle.Alloc(this);
        EnumDisplayMonitors(System.IntPtr.Zero, System.IntPtr.Zero, EnumMonitorsProc, GCHandle.ToIntPtr(gch));
        gch.Free();

        DebugLog($"Enumerated {monitors.Count} monitors");
#else
        // For non-Windows platforms, we can only detect a limited number of displays
        monitorCount = Display.displays.Length;
        DebugLog($"Detected {monitorCount} monitors (non-Windows platform)");
#endif
    }

    public void SwitchToNextMonitor()
    {
        int nextIndex = (currentMonitorIndex + 1) % monitorCount;
        SwitchToMonitor(nextIndex);
    }

    public void SwitchToPreviousMonitor()
    {
        int prevIndex = (currentMonitorIndex - 1 + monitorCount) % monitorCount;
        SwitchToMonitor(prevIndex);
    }

    public void SwitchToMonitor(int monitorIndex)
    {
        if (monitorIndex < 0 || monitorIndex >= monitorCount)
        {
            DebugLog($"Invalid monitor index: {monitorIndex}. Valid range: 0-{monitorCount - 1}");
            return;
        }

        DebugLog($"Switching to monitor {monitorIndex}");

#if UNITY_STANDALONE_WIN
        if (monitors.Count <= monitorIndex)
        {
            DebugLog($"Monitor info not available for index {monitorIndex}");
            return;
        }

        // Get Unity window handle
        System.IntPtr windowHandle = FindWindow(null, Application.productName);
        if (windowHandle == System.IntPtr.Zero)
        {
            DebugLog("Could not find application window");
            return;
        }

        // Get current window rectangle
        RECT windowRect;
        if (!GetWindowRect(windowHandle, out windowRect))
        {
            DebugLog("Failed to get window rect");
            return;
        }

        int currentWidth = windowRect.Width;
        int currentHeight = windowRect.Height;

        // Get target monitor info
        MONITORINFO targetMonitor = monitors[monitorIndex];

        // Calculate new position
        int newX = targetMonitor.rcMonitor.left;
        int newY = targetMonitor.rcMonitor.top;

        // Calculate new size if preserving aspect ratio
        int newWidth = currentWidth;
        int newHeight = currentHeight;

        if (preserveAspectRatio)
        {
            // Maintain aspect ratio but fit within monitor
            float aspectRatio = (float)currentWidth / currentHeight;
            newWidth = targetMonitor.rcMonitor.Width;
            newHeight = Mathf.RoundToInt(newWidth / aspectRatio);

            // If height exceeds monitor height, recalculate
            if (newHeight > targetMonitor.rcMonitor.Height)
            {
                newHeight = targetMonitor.rcMonitor.Height;
                newWidth = Mathf.RoundToInt(newHeight * aspectRatio);
            }

            // Center within monitor
            newX = targetMonitor.rcMonitor.left + (targetMonitor.rcMonitor.Width - newWidth) / 2;
            newY = targetMonitor.rcMonitor.top + (targetMonitor.rcMonitor.Height - newHeight) / 2;
        }
        else if (setFullscreenOnSwitch)
        {
            // Use full monitor dimensions
            newWidth = targetMonitor.rcMonitor.Width;
            newHeight = targetMonitor.rcMonitor.Height;
        }

        // If we want borderless window, remove window styles
        if (setBorderlessOnSwitch)
        {
            MakeWindowBorderless(windowHandle);
        }

        // Move window
        uint flags = SWP_SHOWWINDOW;
        if (setFullscreenOnSwitch)
        {
            // Adjust window size
            flags |= SWP_NOZORDER;
        }
        else if (setBorderlessOnSwitch)
        {
            // Adjust window size
            flags |= SWP_NOZORDER;
        }
        else
        {
            // Keep window size the same
            flags |= SWP_NOSIZE | SWP_NOZORDER;
        }

        bool result = SetWindowPos(windowHandle, System.IntPtr.Zero, newX, newY, newWidth, newHeight, flags);

        if (result)
        {
            currentMonitorIndex = monitorIndex;
            DebugLog($"Successfully moved window to monitor {monitorIndex}: {newX},{newY} - {newWidth}x{newHeight}");

            // Trigger event if assigned
            if (onMonitorChanged != null)
            {
                onMonitorChanged.Invoke(monitorIndex);
            }
        }
        else
        {
            DebugLog("Failed to move window");
        }
#else
        DebugLog("Monitor switching on this platform is not fully supported");
#endif
    }

    /// <summary>
    /// Makes the window borderless by removing caption and frame styles
    /// </summary>
    /// <param name="windowHandle">Optional window handle. If null, finds the handle automatically.</param>
    private void MakeWindowBorderless(System.IntPtr windowHandle = default)
    {
#if UNITY_STANDALONE_WIN
        // Find window handle if not provided
        if (windowHandle == System.IntPtr.Zero)
        {
            windowHandle = FindWindow(null, Application.productName);
            if (windowHandle == System.IntPtr.Zero)
            {
                DebugLog("Could not find application window");
                return;
            }
        }

        // Remove window styles (caption and thick frame)
        System.IntPtr style = GetWindowLong(windowHandle, GWL_STYLE);
        style = new System.IntPtr(style.ToInt64() & ~(WS_CAPTION | WS_THICKFRAME));
        System.IntPtr result = SetWindowLong(windowHandle, GWL_STYLE, style);

        if (result != System.IntPtr.Zero)
        {
            DebugLog("Successfully made window borderless");
        }
        else
        {
            DebugLog("Failed to make window borderless");
        }
#else
        DebugLog("Borderless window is not supported on this platform");
#endif
    }

    /// <summary>
    /// Get details about all connected monitors
    /// </summary>
    /// <returns>Array of monitor info (position, size, work area)</returns>
    public MonitorInfo[] GetMonitorInfo()
    {
#if UNITY_STANDALONE_WIN
        MonitorInfo[] result = new MonitorInfo[monitors.Count];

        for (int i = 0; i < monitors.Count; i++)
        {
            MONITORINFO mi = monitors[i];
            result[i] = new MonitorInfo
            {
                Index = i,
                X = mi.rcMonitor.left,
                Y = mi.rcMonitor.top,
                Width = mi.rcMonitor.Width,
                Height = mi.rcMonitor.Height,
                WorkAreaX = mi.rcWork.left,
                WorkAreaY = mi.rcWork.top,
                WorkAreaWidth = mi.rcWork.Width,
                WorkAreaHeight = mi.rcWork.Height,
                IsPrimary = (mi.dwFlags & 1) != 0 // MONITORINFOF_PRIMARY = 1
            };
        }

        return result;
#else
        return new MonitorInfo[0];
#endif
    }

    /// <summary>
    /// Structure containing monitor information that can be accessed from other scripts
    /// </summary>
    public struct MonitorInfo
    {
        public int Index;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int WorkAreaX;
        public int WorkAreaY;
        public int WorkAreaWidth;
        public int WorkAreaHeight;
        public bool IsPrimary;
    }

    /// <summary>
    /// Returns the current active monitor index
    /// </summary>
    public int CurrentMonitorIndex => currentMonitorIndex;

    /// <summary>
    /// Returns the total number of monitors detected
    /// </summary>
    public int MonitorCount => monitorCount;

    /// <summary>
    /// Change the window mode to borderless fullscreen
    /// </summary>
    public void SetBorderlessFullscreen()
    {
#if UNITY_STANDALONE_WIN
        if (monitors.Count <= currentMonitorIndex)
        {
            DebugLog("Monitor info not available");
            return;
        }

        System.IntPtr windowHandle = FindWindow(null, Application.productName);
        if (windowHandle == System.IntPtr.Zero)
        {
            DebugLog("Could not find application window");
            return;
        }

        MONITORINFO monitor = monitors[currentMonitorIndex];

        // Make borderless
        MakeWindowBorderless(windowHandle);

        // Set size to monitor size
        SetWindowPos(
            windowHandle,
            System.IntPtr.Zero,
            monitor.rcMonitor.left,
            monitor.rcMonitor.top,
            monitor.rcMonitor.Width,
            monitor.rcMonitor.Height,
            SWP_SHOWWINDOW | SWP_NOZORDER
        );
#endif
    }

    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[MonitorManager] {message}");
        }
    }
}