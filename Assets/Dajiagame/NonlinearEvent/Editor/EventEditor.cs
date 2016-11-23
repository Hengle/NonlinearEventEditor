﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dajiagame.NonlinearEvent.Editor
{
    public class EventEditor : EditorWindow
    {
        public static EventEditor Instance;

        private float _sliderValue = 1;

        private Rect _canvasRect = new Rect(0, 0, 20480, 20480);

        private int _gridSize = 64;

        private Vector2 _nodeSize = new Vector2(256, 192);

        private Vector2 _rootNodePositon = new Vector2(256, 128);

        private Vector2 _offset = Vector2.zero;

        private GUISkin _skin;

        public NonlinearEventGroup EventGroup;

        private string _eventGroupFilePath;

        private EventNode _selectedNode;

        [MenuItem("DajiaGame/非线性事件编辑器")]
        static void OnInit()
        {
             GetWindow<EventEditor>("非线性事件编辑器");
        }

        void OnEnable()
        {
            Instance = this;
            _skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Dajiagame/NonlinearEvent/Editor/UI/NonlinearEventEditor.guiskin");
        }

        void OnGUI()
        {
            Background();
            GUI.skin = _skin;
            ShowRightMouseMenu();
            DrawEventNodes();
            GUI.skin = null;
            Repaint();
        }

        private void DrawEventNodes()
        {
            if (EventGroup == null)
            {
                return;
            }

            foreach (var eventNode in EventGroup.ListNodes)
            {
                Node(eventNode);
            }
        }

        private void ShowRightMouseMenu()
        {
            Event currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.MouseUp:
                    if (currentEvent.button == 1)
                    {
                        if (_selectedNode != null && PointInNodeDrawRect(_selectedNode, Event.current.mousePosition))
                        {
                            ShowNodeMenu();
                        }
                        else
                        {
                            ShowSystemMenu(currentEvent);
                        }
                    }
                    break;
            }
        }

        #region background

        private void Background()
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            switch (Event.current.GetTypeForControl(controlID)) {
                case EventType.Repaint:
                    DrawBackground();
                    break;
                case EventType.MouseDrag:
                    if (Event.current.button == 2) {
                        //中键拖拽整个工作区
                        _offset += Event.current.delta;
                    }
                    break;

            }
        }

        private void DrawBackground()
        {
            Rect drawRect = new Rect(_canvasRect.position + _offset, _canvasRect.size);
            GUIStyle canvasBackground = "flow background";
            canvasBackground.Draw(drawRect, false, false, false, false);
            Utils.DrawGrid(drawRect, _gridSize);
        }

        #endregion

        #region node

        private Rect GetNodeDrawRect(EventNode node)
        {
            Rect controlRect = new Rect(node.Position, _nodeSize);
            return new Rect(controlRect.position + _offset, controlRect.size);
        }

        private bool PointInNodeDrawRect(EventNode node, Vector2 point)
        {
            return GetNodeDrawRect(node).Contains(point);
        }

        private void Node(EventNode node)
        {
            Rect controlRect = new Rect(node.Position, _nodeSize);
            Rect drawRect = GetNodeDrawRect(node);

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            switch (Event.current.GetTypeForControl(controlID)) {
                case EventType.Repaint:
                    DrawNode(node, drawRect);
                    break;

                case EventType.MouseDown:
                    if (PointInNodeDrawRect(node, Event.current.mousePosition) && Event.current.button == 0) {
                        Debug.Log("Down At "+ node.ID);
                        GUIUtility.hotControl = controlID;
                    }
                    break;
                case EventType.MouseUp:
                    if (PointInNodeDrawRect(node, Event.current.mousePosition))
                    {
                        if (Event.current.button == 0) {
                            Debug.Log("Up At " + node.ID);
                            if (GUIUtility.hotControl == controlID)
                            {
                                //左键抬起
                                //拖拽完成
                                GUIUtility.hotControl = 0;
                                node.Position = new Vector2((int) (controlRect.x + _gridSize/2)/_gridSize*_gridSize,
                                    (int) (controlRect.y + _gridSize/2)/_gridSize*_gridSize);
                                EditorUtility.SetDirty(EventGroup);
                                //选中Node
                                _selectedNode = node;
                            }
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID) {
                        node.Position += Event.current.delta;
                    }
                    break;
            }
        }

        private void DrawNode(EventNode node, Rect controlRect)
        {
            if (node == _selectedNode) {
                GUI.color = Color.cyan;
            }

            Rect drawRect = new Rect(controlRect.x + 16, controlRect.y + 16, controlRect.width - 32, controlRect.height - 32);           
            GUI.Label(drawRect, "", _skin.GetStyle("Node"));
            GUI.Label(new Rect(drawRect.x, drawRect.y, drawRect.width, 24), "这里是标题", _skin.GetStyle("Title"));
            GUI.TextArea(new Rect(drawRect.x, drawRect.y + 23, drawRect.width, 64),
                "本电子邮件为系统自动发送，请勿直接回复。如有问题，请回复邮箱至", _skin.GetStyle("Talk"));

            GUI.color = Color.white;
        }

        private void ShowNodeMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("←连接"), false, delegate {

            });
            menu.AddItem(new GUIContent("→连接"), false, delegate {

            });
            menu.ShowAsContext();
        }

        private void CreateNewNode(Vector2 createAt)
        {
            if (EventGroup.ListNodes == null)
            {
                EventGroup.ListNodes = new List<EventNode>();
            }
            EventNode newNode = new EventNode
            {
                Position = createAt
            };
            EventGroup.ListNodes.Add(newNode);
        }

        #endregion

        private void ShowSystemMenu(Event currentEvent)
        {
            GenericMenu menu = new GenericMenu();

            if (EventGroup != null) {
                menu.AddItem(new GUIContent("新建节点"), false, delegate
                {
                    CreateNewNode(currentEvent.mousePosition);
                });
                menu.AddItem(new GUIContent("调整设置"), false, EditConfigFile);
                menu.AddSeparator("");
            }

            menu.AddItem(new GUIContent("新建文件"), false, CreateNewFile);
            menu.AddItem(new GUIContent("读取文件"), false, LoadFile);

            if (EventGroup != null) {
                menu.AddItem(new GUIContent("保存文件"), false, delegate { });
                menu.AddItem(new GUIContent("另存为…"), false, delegate { });
            }

            menu.ShowAsContext();
        }

#region ConfigFile

        private void EditConfigFile()
        {
            GetWindow<ConfigEditor>("编辑配置");
        }

#endregion

#region EventGroupFile

        private void CreateNewFile()
        {
            string path = EditorUtility.SaveFilePanel(
                    "选择保存位置", "Assets", "event_group.asset", "asset");
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            EventGroup = CreateInstance<NonlinearEventGroup>();
            _eventGroupFilePath = Utils.AbsolutePathToAssetDataBasePath(path);
            AssetDatabase.CreateAsset(EventGroup, _eventGroupFilePath);
            AssetDatabase.SaveAssets();
            EditConfigFile();
        }

        private void LoadFile()
        {
            string path = EditorUtility.OpenFilePanel("选择文件", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            _eventGroupFilePath = Utils.AbsolutePathToAssetDataBasePath(path);
            EventGroup = AssetDatabase.LoadAssetAtPath<NonlinearEventGroup>(_eventGroupFilePath);
        }

#endregion

    }
}
