﻿using System;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MultiFloatControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        string m_SubLabel1;
        string m_SubLabel2;
        string m_SubLabel3;
        string m_SubLabel4;

        public MultiFloatControlAttribute(string label = null, string subLabel1 = "X", string subLabel2 = "Y", string subLabel3 = "Z", string subLabel4 = "W")
        {
            m_SubLabel1 = subLabel1;
            m_SubLabel2 = subLabel2;
            m_SubLabel3 = subLabel3;
            m_SubLabel4 = subLabel4;
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!MultiFloatControlView.validTypes.Contains(propertyInfo.PropertyType))
                return null;
            return new MultiFloatControlView(m_Label, m_SubLabel1, m_SubLabel2, m_SubLabel3, m_SubLabel4, node, propertyInfo);
        }
    }

    public class MultiFloatControlView : VisualElement
    {
        public static Type[] validTypes = { typeof(float), typeof(Vector2), typeof(Vector3), typeof(Vector4) };

        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        Vector4 m_Value;
        int m_UndoGroup = -1;

        public MultiFloatControlView(string label, string subLabel1, string subLabel2, string subLabel3, string subLabel4, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            int components;
            if (propertyInfo.PropertyType == typeof(float))
                components = 1;
            else if (propertyInfo.PropertyType == typeof(Vector2))
                components = 2;
            else if (propertyInfo.PropertyType == typeof(Vector3))
                components = 3;
            else if (propertyInfo.PropertyType == typeof(Vector4))
                components = 4;
            else
                throw new ArgumentException("Property must be of type float, Vector2, Vector3 or Vector4.", "propertyInfo");

            m_Node = node;
            m_PropertyInfo = propertyInfo;

            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);
            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            m_Value = GetValue();
            AddField(0, subLabel1);
            if (components > 1)
                AddField(1, subLabel2);
            if (components > 2)
                AddField(2, subLabel3);
            if (components > 3)
                AddField(3, subLabel4);
        }

        void AddField(int index, string subLabel)
        {
            Add(new Label(subLabel));
            var doubleField = new DoubleField { userData = index, value = m_Value[index] };
            doubleField.RegisterCallback<MouseDownEvent>(Repaint);
            doubleField.RegisterCallback<MouseMoveEvent>(Repaint);
            doubleField.OnValueChanged(evt =>
            {
                var value = GetValue();
                value[index] = (float)evt.newValue;
                SetValue(value);
                m_UndoGroup = -1;
//                Dirty(ChangeType.Repaint);
            });
            doubleField.RegisterCallback<InputEvent>(evt =>
            {
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                }
                float newValue;
                if (!float.TryParse(evt.newData, out newValue))
                    newValue = 0f;
                var value = GetValue();
                value[index] = newValue;
                SetValue(value);
            });
            doubleField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(m_UndoGroup);
                    m_UndoGroup = -1;
                    m_Value = GetValue();
                    evt.StopPropagation();
                }
                Dirty(ChangeType.Repaint);
            });
            Add(doubleField);
        }

        object ValueToPropertyType(Vector4 value)
        {
            if (m_PropertyInfo.PropertyType == typeof(float))
                return value.x;
            if (m_PropertyInfo.PropertyType == typeof(Vector2))
                return (Vector2)value;
            if (m_PropertyInfo.PropertyType == typeof(Vector3))
                return (Vector3)value;
            return value;
        }

        Vector4 GetValue()
        {
            var value = m_PropertyInfo.GetValue(m_Node, null);
            if (m_PropertyInfo.PropertyType == typeof(float))
                return new Vector4((float) value, 0f, 0f, 0f);
            if (m_PropertyInfo.PropertyType == typeof(Vector2))
                return (Vector2)value;
            if (m_PropertyInfo.PropertyType == typeof(Vector3))
                return (Vector3)value;
            return (Vector4)value;
        }

        void SetValue(Vector4 value)
        {
            m_PropertyInfo.SetValue(m_Node, ValueToPropertyType(value), null);
        }

        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            Dirty(ChangeType.Repaint);
        }
    }
}