using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARFace))]
public class FaceMeshVisualizerExtras : MonoBehaviour
{
    [Header("Eye Gizmos")]
    [SerializeField] bool _showEyeGizmos = true;
    [SerializeField] float _eyeGizmoRadius = 0.005f;
    [SerializeField] Color _leftEyeColor = Color.red;
    [SerializeField] Color _rightEyeColor = Color.blue;
    [SerializeField] Color _gazeColor = Color.yellow;

    [Header("Normals")]
    [SerializeField] bool _showNormals = false;
    [SerializeField] float _normalLength = 0.003f;
    [SerializeField] Color _normalColor = Color.green;

    [Header("Bounds")]
    [SerializeField] bool _showBounds = false;
    [SerializeField] Color _boundsColor = new(1f, 1f, 1f, 0.3f);

    ARFace _face;

    void Awake()
    {
        _face = GetComponent<ARFace>();
    }

    void OnDrawGizmos()
    {
        if (_face == null) return;

        if (_showEyeGizmos)
        {
            DrawEye(_face.leftEye, _leftEyeColor);
            DrawEye(_face.rightEye, _rightEyeColor);

            if (_face.fixationPoint != null)
            {
                Gizmos.color = _gazeColor;
                Gizmos.DrawSphere(_face.fixationPoint.position, _eyeGizmoRadius * 1.5f);
                Gizmos.DrawRay(_face.fixationPoint.position, _face.fixationPoint.forward * 0.05f);
            }
        }

        if (_showNormals && _face.normals.IsCreated && _face.vertices.IsCreated)
        {
            Gizmos.color = _normalColor;
            var verts = _face.vertices;
            var norms = _face.normals;
            int count = Mathf.Min(verts.Length, norms.Length);
            for (int i = 0; i < count; i += 20)
            {
                Vector3 wp = transform.TransformPoint(verts[i]);
                Vector3 wn = transform.TransformDirection(norms[i]);
                Gizmos.DrawRay(wp, wn * _normalLength);
            }
        }

        if (_showBounds && _face.vertices.IsCreated)
        {
            Gizmos.color = _boundsColor;
            var verts = _face.vertices;
            if (verts.Length > 0)
            {
                Bounds b = new Bounds(verts[0], Vector3.zero);
                for (int i = 1; i < verts.Length; i++)
                    b.Encapsulate(verts[i]);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(b.center, b.size);
            }
        }
    }

    void DrawEye(Transform eye, Color color)
    {
        if (eye == null) return;
        Gizmos.color = color;
        Gizmos.DrawSphere(eye.position, _eyeGizmoRadius);
        Gizmos.DrawRay(eye.position, eye.forward * 0.02f);
    }
}