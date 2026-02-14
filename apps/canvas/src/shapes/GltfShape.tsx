import { Suspense, useState } from 'react'
import {
  BaseBoxShapeUtil,
  HTMLContainer,
  RecordProps,
  T,
  TLBaseShape,
  TLResizeInfo,
  resizeBox,
} from 'tldraw'
import { Canvas } from '@react-three/fiber'
import { OrbitControls, Environment, useGLTF, Center } from '@react-three/drei'
import * as THREE from 'three'

// --- Shape Type ---
export type GltfShape = TLBaseShape<
  'gltf',
  {
    w: number
    h: number
    src: string
    label: string
    autoRotate: boolean
    backgroundColor: string
    cameraPosition: number[]
  }
>

// --- 3D Model Component ---
function GltfModel({ src, autoRotate }: { src: string; autoRotate: boolean }) {
  const { scene } = useGLTF(src)

  // Clone the scene so each instance is independent
  const clonedScene = scene.clone(true)

  return (
    <Center>
      <primitive
        object={clonedScene}
        dispose={null}
      />
    </Center>
  )
}

// --- Error Boundary for 3D loading ---
function ModelErrorFallback({ src }: { src: string }) {
  return (
    <div className="gltf-shape__error">
      Failed to load model: {src}
    </div>
  )
}

// --- Inner R3F Canvas ---
function GltfViewer({
  src,
  autoRotate,
  backgroundColor,
  cameraPosition,
}: {
  src: string
  autoRotate: boolean
  backgroundColor: string
  cameraPosition: number[]
}) {
  const [error, setError] = useState(false)

  if (error) {
    return <ModelErrorFallback src={src} />
  }

  return (
    <Canvas
      camera={{
        position: new THREE.Vector3(
          cameraPosition[0] ?? 0,
          cameraPosition[1] ?? 1.5,
          cameraPosition[2] ?? 3
        ),
        fov: 50,
      }}
      style={{ background: backgroundColor }}
      onCreated={({ gl }) => {
        gl.toneMapping = THREE.ACESFilmicToneMapping
        gl.toneMappingExposure = 1.2
      }}
    >
      <ambientLight intensity={0.5} />
      <directionalLight position={[5, 5, 5]} intensity={1} castShadow />

      <Suspense
        fallback={null}
      >
        <GltfModel src={src} autoRotate={autoRotate} />
        <Environment preset="studio" />
      </Suspense>

      <OrbitControls
        enablePan={false}
        autoRotate={autoRotate}
        autoRotateSpeed={2}
        makeDefault
      />
    </Canvas>
  )
}

// --- ShapeUtil ---
export class GltfShapeUtil extends BaseBoxShapeUtil<GltfShape> {
  static override type = 'gltf' as const

  static override props: RecordProps<GltfShape> = {
    w: T.number,
    h: T.number,
    src: T.string,
    label: T.string,
    autoRotate: T.boolean,
    backgroundColor: T.string,
    cameraPosition: T.arrayOf(T.number),
  }

  getDefaultProps(): GltfShape['props'] {
    return {
      w: 400,
      h: 350,
      src: '',
      label: '3D Model',
      autoRotate: true,
      backgroundColor: '#1a1a2e',
      cameraPosition: [0, 1.5, 3],
    }
  }

  component(shape: GltfShape) {
    const { src, label, autoRotate, backgroundColor, cameraPosition } = shape.props

    return (
      <HTMLContainer
        id={shape.id}
        style={{
          width: shape.props.w,
          height: shape.props.h,
          pointerEvents: 'all',
        }}
      >
        <div className="gltf-shape" style={{ '--gltf-bg': backgroundColor } as React.CSSProperties}>
          <div className="gltf-shape__canvas">
            {src ? (
              <GltfViewer
                src={src}
                autoRotate={autoRotate}
                backgroundColor={backgroundColor}
                cameraPosition={cameraPosition}
              />
            ) : (
              <div className="gltf-shape__loading">
                Drop a .glb file or set src
              </div>
            )}
          </div>
          {label && <div className="gltf-shape__label">{label}</div>}
        </div>
      </HTMLContainer>
    )
  }

  indicator(shape: GltfShape) {
    return (
      <rect
        width={shape.props.w}
        height={shape.props.h}
        rx={8}
        ry={8}
      />
    )
  }

  override onResize(shape: GltfShape, info: TLResizeInfo<GltfShape>) {
    return resizeBox(shape, info)
  }
}
