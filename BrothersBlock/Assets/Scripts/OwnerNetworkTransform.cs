using Unity.Netcode.Components;

namespace BrothersBlock
{
    // Responsive owner movement for a private, trusted two-player LAN prototype.
    // A public competitive game would need server validation and prediction.
    public sealed class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() { return false; }
    }
}
