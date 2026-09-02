using UnityEngine;

namespace CrystalSprint
{
    public enum EnvironmentAssetKind { Tree, Stump, Rock, Cliff, Log, Mushroom, Branch, Bush, Water }

    // Records provenance and the terrain contact used by the scene integration and its checks.
    public sealed class EnvironmentAssetInstance : MonoBehaviour
    {
        [SerializeField] private EnvironmentAssetKind kind;
        [SerializeField] private string sourcePrefab;
        [SerializeField] private Vector3 groundContact;
        public EnvironmentAssetKind Kind => kind;
        public string SourcePrefab => sourcePrefab;
        public Vector3 GroundContact => groundContact;

        public void Configure(EnvironmentAssetKind category, string source, Vector3 contact)
        {
            kind = category;
            sourcePrefab = source;
            groundContact = contact;
        }
    }
}
