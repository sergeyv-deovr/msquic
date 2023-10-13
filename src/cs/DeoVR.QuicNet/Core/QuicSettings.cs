using Microsoft.Quic;

namespace DeoVR.QuicNet.Core
{
    public class QuicSettings
    {
        public QuicVersion Version { get; set; } = QuicVersion.Default;

        public string CustomAlpn { get; set; } = string.Empty;

        public QUIC_CREDENTIAL_FLAGS CredentialFlags { get; set; } = QUIC_CREDENTIAL_FLAGS.CLIENT;
    }
}
