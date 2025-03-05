using UnityEngine;

namespace VAT.Animation
{
    [CreateAssetMenu(fileName = "VAT Animation", menuName = "VAT/Animation/VAT Animation")]
    public class VATAnimation : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }

        [field: SerializeField] public Texture2D PositionTexture { get; private set; }

        [field: SerializeField] public Texture2D NormalTexture { get; private set; }

        [field: SerializeField] public float Length { get; private set; }

        [field: SerializeField] public float FPS { get; private set; }

        [field: SerializeField] public bool Loop { get; private set; }

        [field: SerializeField] public VATAnimation OnFinishAnimation { get; private set; }
    }
}