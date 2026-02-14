import {
  BaseBoxShapeUtil,
  HTMLContainer,
  RecordProps,
  T,
  TLBaseShape,
  TLResizeInfo,
  resizeBox,
} from 'tldraw'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'

// --- Shape Type ---
export type MarkdownShape = TLBaseShape<
  'markdown',
  {
    w: number
    h: number
    markdown: string
    color: string
  }
>

// --- ShapeUtil ---
export class MarkdownShapeUtil extends BaseBoxShapeUtil<MarkdownShape> {
  static override type = 'markdown' as const

  static override props: RecordProps<MarkdownShape> = {
    w: T.number,
    h: T.number,
    markdown: T.string,
    color: T.string,
  }

  getDefaultProps(): MarkdownShape['props'] {
    return {
      w: 400,
      h: 300,
      markdown: '# New Note\n\nStart writing here...',
      color: '#ffffff',
    }
  }

  component(shape: MarkdownShape) {
    return (
      <HTMLContainer
        id={shape.id}
        style={{
          width: shape.props.w,
          height: shape.props.h,
          pointerEvents: 'all',
        }}
      >
        <div
          className="markdown-shape"
          style={{ background: shape.props.color }}
        >
          <div className="markdown-shape__content">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {shape.props.markdown}
            </ReactMarkdown>
          </div>
        </div>
      </HTMLContainer>
    )
  }

  indicator(shape: MarkdownShape) {
    return (
      <rect
        width={shape.props.w}
        height={shape.props.h}
        rx={8}
        ry={8}
      />
    )
  }

  override onResize(shape: MarkdownShape, info: TLResizeInfo<MarkdownShape>) {
    return resizeBox(shape, info)
  }
}
