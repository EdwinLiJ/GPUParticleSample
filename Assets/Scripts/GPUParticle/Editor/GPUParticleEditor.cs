using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EdwinTools.Rendering {
    [CustomEditor(typeof(GPUParticle), true)]
    [CanEditMultipleObjects]
    public class GPUParticleEditor : Editor {
        private static string LANGUAGE_VERSION_KEY = "GPU_PARTICLE_EDITOR_LANGUAGE_KEY";
        private static bool s_useEnglishVersion;

        private GPUParticle m_gpuParticleScript;
        
        private SerializedProperty m_playOnEnable;
        private SerializedProperty m_minSpeed;
        private SerializedProperty m_maxSpeed;
        private SerializedProperty m_particleMapSizeAndNum;
        private SerializedProperty m_randomDirectionScale;
        private SerializedProperty m_color;
        private SerializedProperty m_moveType;
        private SerializedProperty m_emitterPosition;
        private SerializedProperty m_emitterCube;
        private SerializedProperty m_throttle;
        private SerializedProperty m_particleLifeTime;
        private SerializedProperty m_randomSeed;
        private SerializedProperty m_targetPosition;
        private SerializedProperty m_emitDirection;
        private SerializedProperty m_rotateAngleRange;
        private SerializedProperty m_startPosMap;
        private SerializedProperty m_startPosMapYScale;
        private SerializedProperty m_startPosMapYThreshold;
        private SerializedProperty m_startPosMapBlockCount;
        

        protected virtual void OnEnable() {
            s_useEnglishVersion = PlayerPrefs.GetInt(LANGUAGE_VERSION_KEY, 0) == 1;

            m_gpuParticleScript = serializedObject.targetObject as GPUParticle;
            m_playOnEnable = serializedObject.FindProperty("m_playOnEnable");
            m_minSpeed = serializedObject.FindProperty("m_minSpeed");
            m_maxSpeed = serializedObject.FindProperty("m_maxSpeed");
            m_particleMapSizeAndNum = serializedObject.FindProperty("m_particleMapSizeAndNum");
            m_color = serializedObject.FindProperty("m_color");
            m_moveType = serializedObject.FindProperty("m_moveType");
            m_emitterPosition = serializedObject.FindProperty("m_emitterPosition");
            m_emitterCube = serializedObject.FindProperty("m_emitterCube");
            m_targetPosition = serializedObject.FindProperty("m_targetPosition");
            m_emitDirection = serializedObject.FindProperty("m_emitDirection");
            m_rotateAngleRange = serializedObject.FindProperty("m_rotateAngleRange");
            m_throttle = serializedObject.FindProperty("m_throttle");
            m_particleLifeTime = serializedObject.FindProperty("m_particleLifeTime");
            m_randomSeed = serializedObject.FindProperty("m_randomSeed");
            m_randomDirectionScale = serializedObject.FindProperty("m_randomDirectionScale");
            m_startPosMap = serializedObject.FindProperty("m_startPosMap");
            m_startPosMapYScale = serializedObject.FindProperty("m_startPosMapYScale");
            m_startPosMapYThreshold = serializedObject.FindProperty("m_startPosMapYThreshold");
            m_startPosMapBlockCount = serializedObject.FindProperty("m_startPosMapBlockCount");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            DrawDefaultGUI();
            
            switch ((GPUParticle.ParticleMoveType)m_moveType.enumValueIndex) {
                case GPUParticle.ParticleMoveType.Directional:
                    DrawDirectionalMovingGUI();
                    break;
                case GPUParticle.ParticleMoveType.ToTarget:
                    DrawTowardsTargetMovingGUI();
                    break;
                case GPUParticle.ParticleMoveType.RotateAround:
                    DrawAroundRotateMovingGUI();
                    break;
            }

            if (GUILayout.Button("Play")) {
                m_gpuParticleScript.Play();
            }

            if (GUILayout.Button("Pause")) {
                m_gpuParticleScript.Pause();
            }

            if (GUILayout.Button("Stop")) {
                m_gpuParticleScript.Stop();
            }

            if (GUILayout.Button("ResetState")) {
                m_gpuParticleScript.ResetState();
            }
            serializedObject.ApplyModifiedProperties();
        }


        private void DrawDefaultGUI() {
            EditorGUI.BeginChangeCheck();
            var englishVersion = GUILayout.Toggle(s_useEnglishVersion, "Language: English");
            if (EditorGUI.EndChangeCheck()) {
                s_useEnglishVersion = englishVersion;
                PlayerPrefs.SetInt(LANGUAGE_VERSION_KEY, 1);
            }
            
            
            EditorGUILayout.PropertyField(m_playOnEnable, GetLabelContent("Enable时播放", "Play On Enable"));
            EditorGUILayout.PropertyField(m_moveType, GetLabelContent("粒子移动类型", "Particle Move Type"));
            EditorGUILayout.PropertyField(m_particleMapSizeAndNum, GetLabelContent("粒子最大数量(Buffer Size)", "Particle Num(Buffer Size)"));
            EditorGUILayout.PropertyField(m_minSpeed, GetLabelContent("最小速度", "Min Speed"));
            EditorGUILayout.PropertyField(m_maxSpeed, GetLabelContent("最大速度", "Max Speed"));
            EditorGUILayout.PropertyField(m_color, GetLabelContent("基础颜色", "Base Color"));
            EditorGUILayout.PropertyField(m_randomDirectionScale, GetLabelContent("粒子运动轨迹随机幅度", "Particle Spread Scale"));
            EditorGUILayout.PropertyField(m_emitterPosition, GetLabelContent("粒子发射位置", "Center Position"));
            EditorGUILayout.PropertyField(m_emitterCube, GetLabelContent("粒子初始位置包围盒", "Particle Born Position Bounds"));
            EditorGUILayout.PropertyField(m_throttle, GetLabelContent("粒子数量控制", "Number Throttle"));
            EditorGUILayout.PropertyField(m_particleLifeTime, GetLabelContent("粒子生命周期", "Single Particle LifeTime"));
            EditorGUILayout.PropertyField(m_randomSeed, GetLabelContent("随机种子", "Random Seed"));

            EditorGUILayout.PropertyField(m_startPosMap, GetLabelContent("粒子群初始位置采样图", "Map of Particles start shape"));
            if (m_startPosMap.objectReferenceValue as Texture2D) {
                EditorGUILayout.PropertyField(m_startPosMapYScale, GetLabelContent("粒子群Y轴跨度", "Particles Y padding"));
                EditorGUILayout.PropertyField(m_startPosMapBlockCount, GetLabelContent("粒子群Y轴重叠", "Particles Y repeat count"));
                EditorGUILayout.PropertyField(m_startPosMapYThreshold, GetLabelContent("采样图显示阈值", "Particles display threshold"));
            }
        }

        private void DrawDirectionalMovingGUI() {
            EditorGUILayout.PropertyField(m_emitDirection,
                GetLabelContent("粒子移动方向", "Particle Moving Direction"));
        }

        private void DrawTowardsTargetMovingGUI() {
            EditorGUILayout.PropertyField(m_targetPosition,
                GetLabelContent("目标位置", "Target Position"));
        }

        private void DrawAroundRotateMovingGUI() {
            EditorGUILayout.PropertyField(m_rotateAngleRange,
                GetLabelContent("旋转角度放大值", "Centrifugal Force"));
        }

        private GUIContent GetLabelContent(string textCN, string textEN) {
            var label = new GUIContent(s_useEnglishVersion ? textEN : textCN);
            return label;
        }
    }
}