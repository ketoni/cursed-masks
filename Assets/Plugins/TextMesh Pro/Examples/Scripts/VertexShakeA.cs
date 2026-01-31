using UnityEngine;
using System.Collections;
using TMPro;

namespace TMPro.Examples
{
    public class VertexShakeA : MonoBehaviour
    {
        public float SpeedMultiplier = 1.0f;
        public float ScaleMultiplier = 1.0f;
        private TMP_Text m_TextComponent;
        private bool hasTextChanged;

        void Awake()
        {
            m_TextComponent = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            // Subscribe to event fired when text object has been regenerated.
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
        }

        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
        }

        void Start()
        {
            StartCoroutine(AnimateVertexColors());
        }

        void ON_TEXT_CHANGED(Object obj)
        {
            if (obj == m_TextComponent)
                hasTextChanged = true;
        }

        /// <summary>
        /// Method to animate vertex colors of a TMP Text object.
        /// </summary>
        /// <returns></returns>
        IEnumerator AnimateVertexColors()
        {
            // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
            m_TextComponent.ForceMeshUpdate();

            TMP_TextInfo textInfo = m_TextComponent.textInfo;

            Vector3[][] copyOfVertices = new Vector3[0][];

            hasTextChanged = true;

            while (true)
            {
                // Allocate new vertices 
                if (hasTextChanged)
                {
                    if (copyOfVertices.Length < textInfo.meshInfo.Length)
                        copyOfVertices = new Vector3[textInfo.meshInfo.Length][];

                    for (int i = 0; i < textInfo.meshInfo.Length; i++)
                    {
                        int length = textInfo.meshInfo[i].vertices.Length;
                        copyOfVertices[i] = new Vector3[length];
                    }

                    hasTextChanged = false;
                }

                int characterCount = textInfo.characterCount;

                // If no characters then just wait for some text to be added
                if (characterCount == 0)
                {
                    yield return null;
                    continue;
                }

                // Iterate through each character in the text.
                float time = Time.time;
                for (int i = 0; i < characterCount; i++)
                {
                    // Skip characters that are not visible and thus have no geometry to manipulate.
                    if (!textInfo.characterInfo[i].isVisible)
                        continue;

                    // Get the index of the material used by the current character.
                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    // Get the index of the first vertex used by this text element.
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // Get the vertices of the mesh used by this text element (character or sprite).
                    Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

                    // The wave effect is based on current time and this character's index
                    // The scalars are here to create a sensible baseline effect with multipliers of 1.0f
                    float offset = Mathf.Sin(time * SpeedMultiplier * 5f - i) * ScaleMultiplier * 3f;

                    // Need to offset all 4 vertices of each quad
                    for (int j = 0; j < 4; j++)
                    {
                        copyOfVertices[materialIndex][vertexIndex + j] = sourceVertices[vertexIndex + j];
                        copyOfVertices[materialIndex][vertexIndex + j].y += offset;
                    }
                }

                // Push changes into meshes
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = copyOfVertices[i];
                    m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                yield return null;
            }
        }
    }
}