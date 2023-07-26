/*=============================================================
 * GPUParticle(Mobile Suitable Version)
 * --------------------------------
 * This is a Sample to show a different thinking of effect developing.
 * You can use this sample on any project if you want.
 * 该项目为一个示例工程，目的是展示一种不同的思维方式，用来做效果开发
 * 该工程可以直接或间接的用于任何项目
 *
 * https://github.com/EdwinLiJ/GPUParticleSample
 *                      Made By: EdwinLiJ - Github
 *                      知乎:七夜大黑喵 / BiliBili:七夜小黑喵
 ==============================================================*/

#if UNITY_EDITOR
#define _UNITY_EDITOR
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EdwinTools.Rendering {
    public class GPUParticle : MonoBehaviour {
        private static class ShaderIDs {
            public static readonly int EmitterPosID = Shader.PropertyToID("_EmitterPos");
            public static readonly int EmitterSizeID = Shader.PropertyToID("_EmitterSize");
            public static readonly int DirectionID = Shader.PropertyToID("_Direction");
            public static readonly int MinMaxSpeedID = Shader.PropertyToID("_MinMaxSpeed");
            public static readonly int RandomDirectionScaleID = Shader.PropertyToID("_RandomDirectionScale");

            public static readonly int RandomSeedID = Shader.PropertyToID("_RandomSeed");
            public static readonly int ThrottleID = Shader.PropertyToID("_Throttle");
            public static readonly int LifeTimeID = Shader.PropertyToID("_LifeTime");
            public static readonly int CustomDeltaTimeID = Shader.PropertyToID("_CustomDeltaTime");


            public static readonly int TargetPositionID = Shader.PropertyToID("_TargetPosition");
            public static readonly int RotateAngleRangeID = Shader.PropertyToID("_RotateAngleRange");
            public static readonly int NoiseTexID = Shader.PropertyToID("_NoiseTex");
            public static readonly int ParticleDurationID = Shader.PropertyToID("_ParticleDuration");

            public static readonly int LineStripHeadColor = Shader.PropertyToID("_LineStripHeadColor");
            public static readonly int LineStripEndColor = Shader.PropertyToID("_LineStripEndColor");
            public static readonly int LineStripHeadColorInt = Shader.PropertyToID("_LineStripHeadColorInt");
            public static readonly int LineStripHeadPosition = Shader.PropertyToID("_LineStripHeadPosition");


            public static readonly int DisplayMainTexID = Shader.PropertyToID("_MainTex");

            public static readonly int ParticleTexID = Shader.PropertyToID("_ParticleTex");

            // public static readonly int ParticleTex2ID = Shader.PropertyToID("_ParticleTex2");
            public static readonly int TailID = Shader.PropertyToID("_Tail");
            public static readonly int ColorID = Shader.PropertyToID("_Color");
        }

        private const string FIXED_MOVE_DIRECTION = "_FIXED_MOVE_DIRECTION";
        private const string MOVE_TO_TARGET_POSITION = "_MOVE_TO_TARGET_POSITION";

        private const string MOVE_AROUND_TARGET_POSITION = "_MOVE_AROUND_TARGET_POSITION";
        // private const string EXPLOSION_FROM_CENTER = "_EXPLOSION_FROM_CENTER";
        // private const string USE_MESH_LINE = "_USE_MESH_LINE";
        // private const string USE_MESH_LINE_STRIP = "_USE_MESH_LINE_STRIP";

        private enum ParticleMapSizeAndNum {
            Size4x4_16 = 16,
            Size8x8_64 = 64,
            Size16x16_256 = 256,
            Size32x32_1024 = 1024,
            Size64x64_4096 = 4096,
            Size64x128_8192 = 8192,
            Size128x128_16384 = 16384,
            Size1024x1024_1048576 = 1048576
        }

        public enum ParticleMoveType {
            /// <summary>
            /// Move via a Direction
            /// </summary>
            Directional = 0,

            /// <summary>
            /// Move to Target Position
            /// </summary>
            ToTarget = 1,

            /// <summary>
            /// Move around a position
            /// </summary>
            RotateAround = 2
        }

        private static string META_SHADER_NAME = "Hidden/GPUParticleSystem/ParticleMeta";
        private static string DISPLAY_SHADER_NAME = "Hidden/GPUParticleSystem/ParticleDisplay";

        [SerializeField]
        private bool m_playOnEnable = true;

        [SerializeField]
        private float m_minSpeed = 1f;

        [SerializeField]
        private float m_maxSpeed = 2f;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_randomDirectionScale = 0f;

        [SerializeField]
        private ParticleMapSizeAndNum m_particleMapSizeAndNum = ParticleMapSizeAndNum.Size64x128_8192;

        [SerializeField]
        [ColorUsage(false, true)]
        private Color m_color = Color.white;

        // [SerializeField]
        // private Texture2D m_noiseTexture;

        [SerializeField]
        private ParticleMoveType m_moveType = ParticleMoveType.Directional;

        /// <summary>
        /// 发射起点的位置
        /// Position where particles emit
        /// </summary>
        [SerializeField]
        private Vector3 m_emitterPosition = Vector3.zero;

        /// <summary>
        /// The bounds of emitter start position
        /// </summary>
        [SerializeField]
        private Vector3 m_emitterCube = Vector3.one * 50f;

        /// <summary>
        /// 粒子系统发射移动向具体位置
        /// Position where particles move to
        /// </summary>
        [SerializeField]
        private Vector3 m_targetPosition = new Vector3(0, 0, -10);

        /// <summary>
        /// 粒子系统发射的方向
        /// Particles move direction
        /// </summary>
        [SerializeField]
        private Vector3 m_emitDirection = Vector3.down;

        [SerializeField]
        [Range(0.03f, 0.3f)]
        private float m_rotateAngleRange = 0.05f;

        /// <summary>
        /// 决定显示多少粒子
        /// Determine how many particles display
        /// </summary>
        [SerializeField]
        [Range(0f, 1f)]
        private float m_throttle = 0f;

        /// <summary>
        /// 单个粒子的生命周期
        /// </summary>
        [SerializeField]
        private float m_particleLifeTime = 1f;

        [SerializeField]
        private int m_randomSeed;

        private Mesh m_mesh;
        private RenderTexture m_particleMetaAttachmentA;
        private RenderTexture m_particleMetaAttachmentB;
        private Material m_metaMaterial;
        private Material m_displayMaterial;

        private bool m_isPlaying;
        private bool m_isPause;


        private void OnEnable() {
            SetupBuffer(m_particleMapSizeAndNum);
            if (m_playOnEnable) {
                Play();
            }
        }

        private void OnDisable() {
            Stop();
            ClearBuffer();
        }

        private void Update() {
            var deltaTime = 0f;
            if (m_isPlaying) {
                deltaTime = m_isPause ? 0f : Time.deltaTime;
            }
            else {
                return;
            }

            UpdateKeywords();
            UpdateMeta(deltaTime);
            UpdateDisplay(deltaTime);
            Graphics.DrawMesh(m_mesh, transform.position, transform.rotation, m_displayMaterial, gameObject.layer);
        }

        private static void SetKeywordByMoveType(Material material, string keyword, ParticleMoveType currentType,
            ParticleMoveType targetType) {
            if (currentType == targetType) {
                material.EnableKeyword(keyword);
            }
            else {
                material.DisableKeyword(keyword);
            }
        }

        private void UpdateKeywords() {
            SetKeywordByMoveType(m_metaMaterial, FIXED_MOVE_DIRECTION, m_moveType,
                ParticleMoveType.Directional);
            SetKeywordByMoveType(m_metaMaterial, MOVE_TO_TARGET_POSITION, m_moveType,
                ParticleMoveType.ToTarget);
            SetKeywordByMoveType(m_metaMaterial, MOVE_AROUND_TARGET_POSITION, m_moveType,
                ParticleMoveType.RotateAround);
        }

        private void UpdateMeta(float deltaTime) {
            m_metaMaterial.SetVector(ShaderIDs.EmitterPosID, m_emitterPosition);
            m_metaMaterial.SetVector(ShaderIDs.EmitterSizeID, m_emitterCube);
            m_metaMaterial.SetFloat(ShaderIDs.ThrottleID, m_throttle);
            m_metaMaterial.SetFloat(ShaderIDs.LifeTimeID, m_particleLifeTime);
            m_metaMaterial.SetFloat(ShaderIDs.RandomSeedID, m_randomSeed);
            m_metaMaterial.SetFloat(ShaderIDs.CustomDeltaTimeID, deltaTime);
            m_metaMaterial.SetVector(ShaderIDs.MinMaxSpeedID, new Vector4(m_minSpeed, m_maxSpeed));
            m_metaMaterial.SetFloat(ShaderIDs.RandomDirectionScaleID, m_randomDirectionScale);

            switch (m_moveType) {
                case ParticleMoveType.Directional:
                    m_metaMaterial.SetVector(ShaderIDs.DirectionID, m_emitDirection);
                    break;
                case ParticleMoveType.ToTarget:
                    m_metaMaterial.SetVector(ShaderIDs.TargetPositionID, m_targetPosition);
                    break;
                case ParticleMoveType.RotateAround:
                    m_metaMaterial.SetFloat(ShaderIDs.RotateAngleRangeID, m_rotateAngleRange);
                    break;
            }

            // var rt = m_particleMetaAttachmentA;
            // m_particleMetaAttachmentA = m_particleMetaAttachmentB;
            // m_particleMetaAttachmentB = rt;
            (m_particleMetaAttachmentA, m_particleMetaAttachmentB) =
                (m_particleMetaAttachmentB, m_particleMetaAttachmentA);
            Graphics.Blit(m_particleMetaAttachmentA, m_particleMetaAttachmentB, m_metaMaterial, 1);
        }

        private void UpdateDisplay(float deltaTime) {
            m_displayMaterial.SetTexture(ShaderIDs.ParticleTexID, m_particleMetaAttachmentB);
            m_displayMaterial.SetColor(ShaderIDs.ColorID, m_color);
        }

        private void SetupBuffer(ParticleMapSizeAndNum mapSizeAndNum) {
            var width = 4;
            var height = 4;
            switch (mapSizeAndNum) {
                case ParticleMapSizeAndNum.Size4x4_16:
                    width = 4;
                    height = 4;
                    break;
                case ParticleMapSizeAndNum.Size8x8_64:
                    width = 8;
                    height = 8;
                    break;
                case ParticleMapSizeAndNum.Size16x16_256:
                    width = 16;
                    height = 16;
                    break;
                case ParticleMapSizeAndNum.Size32x32_1024:
                    width = 32;
                    height = 32;
                    break;
                case ParticleMapSizeAndNum.Size64x64_4096:
                    width = 64;
                    height = 64;
                    break;
                case ParticleMapSizeAndNum.Size64x128_8192:
                    width = 64;
                    height = 128;
                    break;
                case ParticleMapSizeAndNum.Size128x128_16384:
                    width = 128;
                    height = 128;
                    break;
                case ParticleMapSizeAndNum.Size1024x1024_1048576:
                    width = 1024;
                    height = 1024;
                    break;
            }

            if (m_mesh) {
                DestroyImmediate(m_mesh);
            }

            m_mesh = ParticleMeshUtils.GeneratePointMesh(width, height);

            if (m_particleMetaAttachmentA) {
                DestroyImmediate(m_particleMetaAttachmentA);
            }

            m_particleMetaAttachmentA = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            m_particleMetaAttachmentA.hideFlags = HideFlags.DontSave;
            m_particleMetaAttachmentA.filterMode = FilterMode.Point;
            m_particleMetaAttachmentA.wrapMode = TextureWrapMode.Repeat;

            if (m_particleMetaAttachmentB) {
                DestroyImmediate(m_particleMetaAttachmentB);
            }

            m_particleMetaAttachmentB = new RenderTexture(m_particleMetaAttachmentA.descriptor);

            if (!m_metaMaterial) {
                m_metaMaterial = new Material(Shader.Find(META_SHADER_NAME));
            }

            if (!m_displayMaterial) {
                m_displayMaterial = new Material(Shader.Find(DISPLAY_SHADER_NAME));
                m_displayMaterial.renderQueue = 3000;
            }

            // 初始化MetaRT，生成第一次需要的信息图
            Graphics.Blit(null, m_particleMetaAttachmentB, m_metaMaterial, 0);
        }

        private void ClearBuffer() {
            if (m_mesh) {
                DestroyImmediate(m_mesh);
            }

            if (m_metaMaterial) {
                DestroyImmediate(m_metaMaterial);
            }

            if (m_displayMaterial) {
                DestroyImmediate(m_displayMaterial);
            }

            if (m_particleMetaAttachmentA) {
                DestroyImmediate(m_particleMetaAttachmentA);
            }

            if (m_particleMetaAttachmentB) {
                DestroyImmediate(m_particleMetaAttachmentB);
            }
        }

        public void Play() {
            m_isPlaying = true;
            m_isPause = false;
        }

        public void Pause() {
            m_isPause = true;
        }

        public void Stop() {
            m_isPlaying = false;
            m_isPause = false;
        }

        public void ResetState() {
            SetupBuffer(m_particleMapSizeAndNum);
        }

#if _UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + m_emitterPosition, m_emitterCube);
            switch (m_moveType) {
                case ParticleMoveType.Directional:
                    break;
                case ParticleMoveType.ToTarget:
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(transform.position + m_targetPosition, 2f);
                    break;
                case ParticleMoveType.RotateAround: 
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnGUI() {
            
        }
#endif
    }
}