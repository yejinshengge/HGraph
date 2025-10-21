using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HGraph.Editor
{
    public class HGraphWindow : EditorWindow
    {
        // 视图
        private HGraphView _graphView;

        private HGraphBase _graph;

        public static void Open(HGraphBase graph)
        {
            if (!graph) return;
            graph.OnBeforeOpen();
            bool openNewWindow = Event.current != null && ( Event.current.modifiers & EventModifiers.Alt ) == EventModifiers.Alt;
            HGraphWindow w = openNewWindow ? CreateWindow<HGraphWindow>("HGraph") : GetWindow<HGraphWindow>("HGraph");
            w.wantsMouseMove = true;
            w.titleContent = new GUIContent( graph.name );
            w._graph = graph;
            w.Focus();
            w._onOpen();
        }

        private void _onOpen()
        {
            OnDisable();
            _createGraphView();
        }

        private void OnDisable()
        {
            // 清理资源
            _graphView?.Dispose();
            _graphView = null;
            
            this.rootVisualElement.Clear();
        }

        // 创建视图
        private void _createGraphView()
        {
            _graphView = new HGraphView(this,_graph);
            _graphView.StretchToParentSize();
            this.rootVisualElement.Add(_graphView);
        }
    }
}