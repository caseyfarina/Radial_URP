import { Tldraw, TLComponents } from 'tldraw'
import 'tldraw/tldraw.css'
import { customShapeUtils } from './shapes'
import { customTools } from './tools'
import { uiOverrides } from './ui-overrides'
import './app.css'

const components: TLComponents = {
  // Can add custom UI components here later
}

export default function App() {
  return (
    <div className="app">
      <Tldraw
        shapeUtils={customShapeUtils}
        tools={customTools}
        overrides={uiOverrides}
        components={components}
      />
    </div>
  )
}
