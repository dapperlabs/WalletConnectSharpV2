using WalletConnectSharp.Common;
using WalletConnectSharp.Common.Model.Errors;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Storage;
using Xunit;

namespace WalletConnectSharp.Sign.Test
{
    public class SignTests : IClassFixture<TwoClientsFixture>
    {
        private TwoClientsFixture _cryptoFixture;

        public WalletConnectSignClient ClientA
        {
            get
            {
                return _cryptoFixture.ClientA;
            }
        }
        
        public WalletConnectSignClient ClientB
        {
            get
            {
                return _cryptoFixture.ClientB;
            }
        }

        public SignTests(TwoClientsFixture cryptoFixture)
        {
            this._cryptoFixture = cryptoFixture;
        }

        [Fact]
        public async void TestApproveSession()
        {
            await _cryptoFixture.WaitForClientsReady();
            
            var testAddress = "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045";
            var dappConnectOptions = new ConnectParams()
            {
                RequiredNamespaces = new RequiredNamespaces()
                {
                    {
                        "eip155", new RequiredNamespace()
                        {
                            Methods = new[]
                            {
                                "eth_sendTransaction",
                                "eth_signTransaction",
                                "eth_sign",
                                "personal_sign",
                                "eth_signTypedData",
                            },
                            Chains = new[]
                            {
                                "eip155:1"
                            },
                            Events = new[]
                            {
                                "chainChanged", "accountsChanged"
                            }
                        }
                    }
                }
            };

            var dappClient = ClientA;
            var connectData = await dappClient.Connect(dappConnectOptions);

            var walletClient = ClientB;
            var pairing = await walletClient.Pair(new PairParams()
            {
                Uri = connectData.Uri
            });

            var proposal = await pairing.FetchProposal;

            var approveData = await walletClient.Approve(proposal, testAddress);

            var sessionData = await connectData.Approval;
            await approveData.Acknowledged();
        }
        
        [Fact]
        public async void TestRejectSession()
        {
            await _cryptoFixture.WaitForClientsReady();
            
            var testAddress = "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045";
            var dappConnectOptions = new ConnectParams()
            {
                RequiredNamespaces = new RequiredNamespaces()
                {
                    {
                        "eip155", new RequiredNamespace()
                        {
                            Methods = new[]
                            {
                                "eth_sendTransaction",
                                "eth_signTransaction",
                                "eth_sign",
                                "personal_sign",
                                "eth_signTypedData",
                            },
                            Chains = new[]
                            {
                                "eip155:1"
                            },
                            Events = new[]
                            {
                                "chainChanged", "accountsChanged"
                            }
                        }
                    }
                }
            };

            var dappClient = ClientA;
            var connectData = await dappClient.Connect(dappConnectOptions);

            var walletClient = ClientB;
            var pairing = await walletClient.Pair(new PairParams()
            {
                Uri = connectData.Uri
            });

            var proposal = await pairing.FetchProposal;

            await walletClient.Reject(proposal);

            await Assert.ThrowsAsync<WalletConnectException>(() => connectData.Approval);
        }
    }
}