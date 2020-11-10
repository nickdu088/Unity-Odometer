using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OdoMeter : MonoBehaviour
{
    public TextMeshPro Meter;
    readonly char[] SpinStrip = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    Vector3[] sourceVertices;
    TMP_MeshInfo[] cachedMeshInfo;
    Vector3 centerOfRotation = new Vector3(0, 0, 1.5f);
    // Start is called before the first frame update
    void Start()
    {
        Meter = GetComponent<TextMeshPro>();
        Prepare();
        StartCoroutine("Spin", new Tuple<int, int, float>(0, 18, 1f));
    }

    void Prepare()
    {
        //keep the orginal text vector for future use
        Meter.ForceMeshUpdate();
        sourceVertices = (Vector3[])Meter.textInfo.CopyMeshInfoVertexData()[0].vertices.Clone();
        Meter.textInfo.Clear();
        var templateString = string.Empty;
        foreach (var s in SpinStrip)
        {
            templateString += s + "\n";
        }

        //Update text to spinning text
        Meter.text = templateString;
        Meter.ForceMeshUpdate();
        cachedMeshInfo = Meter.textInfo.CopyMeshInfoVertexData();

        HideSpinCharacters();
        Spin(Meter.textInfo.characterInfo[currentValue], centerOfRotation, 0);
        Apply();
    }

    int currentValue = 0;

    void HideSpinCharacters()
    {
        for (var i = 0; i < Meter.textInfo.characterCount; i++)
        {
            var charInfo = Meter.textInfo.characterInfo[i];
            if (charInfo.isVisible)
            {
                Spin(charInfo, centerOfRotation, -90);
            }
        }
    }

    IEnumerator Spin(Tuple<int, int, float> param)
    {
        for (var curChar = param.Item1; curChar < param.Item2;curChar += 2)
        {
            var curCharInfo = Meter.textInfo.characterInfo[curChar];
            var nextCharInfo = Meter.textInfo.characterInfo[curChar + 2];

            for (float degree = -90; degree <= 0; degree += param.Item3)
            {
                Spin(curCharInfo, centerOfRotation, (degree + 90) < 90 ? (degree + 90) : 90);
                Spin(nextCharInfo, centerOfRotation, degree);
                Apply();
                yield return null;
            }
            yield return null;
        }
    }

    void Spin(TMP_CharacterInfo charInfo, Vector3 centerOfRotation, float angleOfRotation)
    {
        // Get the index of the material used by the current character.
        int materialIndex = charInfo.materialReferenceIndex;

        // Get the index of the first vertex used by this text element.
        int vertexIndex = charInfo.vertexIndex;

        // Get the cached vertices of the mesh used by this text element (character or sprite).

        // Determine the center point of each character at the baseline.
        // Determine the center point of each character.
        Vector2 charMidBasline = (sourceVertices[0] + sourceVertices[2]) / 2;

        // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
        // This is needed so the matrix TRS is applied at the origin for each character.
        Vector3 offset = charMidBasline;

        Vector3[] destinationVertices = Meter.textInfo.meshInfo[materialIndex].vertices;

        destinationVertices[vertexIndex + 0] = sourceVertices[0] - offset;
        destinationVertices[vertexIndex + 1] = sourceVertices[1] - offset;
        destinationVertices[vertexIndex + 2] = sourceVertices[2] - offset;
        destinationVertices[vertexIndex + 3] = sourceVertices[3] - offset;


        // This should calculate the matrix, which helps to roate odometer
        Matrix4x4 translationToCenterPoint = Matrix4x4.Translate(centerOfRotation);
        Matrix4x4 rotation = Matrix4x4.Rotate(Quaternion.AngleAxis(angleOfRotation, Vector3.right));
        Matrix4x4 translationBackToOrigin = Matrix4x4.Translate(-centerOfRotation);

        Matrix4x4 matrix = translationToCenterPoint * rotation * translationBackToOrigin;

        destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
        destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
        destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
        destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

        destinationVertices[vertexIndex + 0] += offset;
        destinationVertices[vertexIndex + 1] += offset;
        destinationVertices[vertexIndex + 2] += offset;
        destinationVertices[vertexIndex + 3] += offset;
    }

    void Apply()
    {
        // Push changes into meshes
        for (int i = 0; i < Meter.textInfo.meshInfo.Length; i++)
        {
            Meter.textInfo.meshInfo[i].mesh.vertices = Meter.textInfo.meshInfo[i].vertices;
            Meter.UpdateGeometry(Meter.textInfo.meshInfo[i].mesh, i);
        }
    }
}
