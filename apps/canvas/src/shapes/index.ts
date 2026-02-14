import { ShapeUtil } from 'tldraw'
import { MarkdownShapeUtil } from './MarkdownShape'
import { GltfShapeUtil } from './GltfShape'

export const customShapeUtils: (typeof ShapeUtil<any>)[] = [
  MarkdownShapeUtil,
  GltfShapeUtil,
]

export { MarkdownShapeUtil, GltfShapeUtil }
export type { MarkdownShape } from './MarkdownShape'
export type { GltfShape } from './GltfShape'
