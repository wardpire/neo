// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Nep17API.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Extensions;
using Neo.Json;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.Network.RPC.Tests
{
    [TestClass]
    public class UT_Nep17API
    {
        Mock<RpcClient> rpcClientMock;
        KeyPair keyPair1;
        UInt160 sender;
        Nep17API nep17API;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            sender = Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
            rpcClientMock = UT_TransactionManager.MockRpcClient(sender, new byte[0]);
            nep17API = new Nep17API(rpcClientMock.Object);
        }

        [TestMethod]
        public async Task TestBalanceOf()
        {
            byte[] testScript = rpcClientMock.Object.NativeContractRepository.GAS.Hash.MakeScript("balanceOf", UInt160.Zero);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(10000) });

            var balance = await nep17API.BalanceOfAsync(rpcClientMock.Object.NativeContractRepository.GAS.Hash, UInt160.Zero);
            Assert.AreEqual(10000, (int)balance);
        }

        [TestMethod]
        public async Task TestGetSymbol()
        {
            byte[] testScript = rpcClientMock.Object.NativeContractRepository.GAS.Hash.MakeScript("symbol");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.String, Value = rpcClientMock.Object.NativeContractRepository.GAS.Symbol });

            var result = await nep17API.SymbolAsync(rpcClientMock.Object.NativeContractRepository.GAS.Hash);
            Assert.AreEqual(rpcClientMock.Object.NativeContractRepository.GAS.Symbol, result);
        }

        [TestMethod]
        public async Task TestGetDecimals()
        {
            byte[] testScript = rpcClientMock.Object.NativeContractRepository.GAS.Hash.MakeScript("decimals");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(rpcClientMock.Object.NativeContractRepository.GAS.Decimals) });

            var result = await nep17API.DecimalsAsync(rpcClientMock.Object.NativeContractRepository.GAS.Hash);
            Assert.AreEqual(rpcClientMock.Object.NativeContractRepository.GAS.Decimals, result);
        }

        [TestMethod]
        public async Task TestGetTotalSupply()
        {
            byte[] testScript = rpcClientMock.Object.NativeContractRepository.GAS.Hash.MakeScript("totalSupply");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            var result = await nep17API.TotalSupplyAsync(rpcClientMock.Object.NativeContractRepository.GAS.Hash);
            Assert.AreEqual(1_00000000, (int)result);
        }

        [TestMethod]
        public async Task TestGetTokenInfo()
        {
            UInt160 scriptHash = rpcClientMock.Object.NativeContractRepository.GAS.Hash;
            byte[] testScript = [
                .. scriptHash.MakeScript("symbol"),
                .. scriptHash.MakeScript("decimals"),
                .. scriptHash.MakeScript("totalSupply")];
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript,
                new ContractParameter { Type = ContractParameterType.String, Value = rpcClientMock.Object.NativeContractRepository.GAS.Symbol },
                new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(rpcClientMock.Object.NativeContractRepository.GAS.Decimals) },
                new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            scriptHash = rpcClientMock.Object.NativeContractRepository.NEO.Hash;
            testScript = [
                .. scriptHash.MakeScript("symbol"),
                .. scriptHash.MakeScript("decimals"),
                .. scriptHash.MakeScript("totalSupply")];
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript,
                new ContractParameter { Type = ContractParameterType.String, Value = rpcClientMock.Object.NativeContractRepository.NEO.Symbol },
                new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(rpcClientMock.Object.NativeContractRepository.NEO.Decimals) },
                new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            var tests = TestUtils.RpcTestCases.Where(p => p.Name == "getcontractstateasync");
            var haveGasTokenUT = false;
            var haveNeoTokenUT = false;
            foreach (var test in tests)
            {
                rpcClientMock.Setup(p => p.RpcSendAsync("getcontractstate", It.Is<JToken[]>(u => true)))
                .ReturnsAsync(test.Response.Result)
                .Verifiable();
                if (test.Request.Params[0].AsString() == rpcClientMock.Object.NativeContractRepository.GAS.Hash.ToString() || test.Request.Params[0].AsString().Equals(rpcClientMock.Object.NativeContractRepository.GAS.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    var result = await nep17API.GetTokenInfoAsync(rpcClientMock.Object.NativeContractRepository.GAS.Name.ToLower());
                    Assert.AreEqual(rpcClientMock.Object.NativeContractRepository.GAS.Symbol, result.Symbol);
                    Assert.AreEqual(8, result.Decimals);
                    Assert.AreEqual(1_00000000, (int)result.TotalSupply);
                    Assert.AreEqual("GasToken", result.Name);

                    result = await nep17API.GetTokenInfoAsync(rpcClientMock.Object.NativeContractRepository.GAS.Hash);
                    Assert.AreEqual(rpcClientMock.Object.NativeContractRepository.GAS.Symbol, result.Symbol);
                    Assert.AreEqual(8, result.Decimals);
                    Assert.AreEqual(1_00000000, (int)result.TotalSupply);
                    Assert.AreEqual("GasToken", result.Name);
                    haveGasTokenUT = true;
                }
                else if (test.Request.Params[0].AsString() == rpcClientMock.Object.NativeContractRepository.NEO.Hash.ToString() || test.Request.Params[0].AsString().Equals(rpcClientMock.Object.NativeContractRepository.NEO.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    var result = await nep17API.GetTokenInfoAsync(rpcClientMock.Object.NativeContractRepository.NEO.Name.ToLower());
                    Assert.AreEqual(rpcClientMock.Object.NativeContractRepository.NEO.Symbol, result.Symbol);
                    Assert.AreEqual(0, result.Decimals);
                    Assert.AreEqual(1_00000000, (int)result.TotalSupply);
                    Assert.AreEqual("NeoToken", result.Name);

                    result = await nep17API.GetTokenInfoAsync(rpcClientMock.Object.NativeContractRepository.NEO.Hash);
                    Assert.AreEqual(rpcClientMock.Object.NativeContractRepository.NEO.Symbol, result.Symbol);
                    Assert.AreEqual(0, result.Decimals);
                    Assert.AreEqual(1_00000000, (int)result.TotalSupply);
                    Assert.AreEqual("NeoToken", result.Name);
                    haveNeoTokenUT = true;
                }
            }
            Assert.IsTrue(haveGasTokenUT && haveNeoTokenUT); //Update RpcTestCases.json
        }

        [TestMethod]
        public async Task TestTransfer()
        {
            byte[] testScript = rpcClientMock.Object.NativeContractRepository.GAS.Hash.MakeScript("transfer", sender, UInt160.Zero, new BigInteger(1_00000000), null)
                .Concat(new[] { (byte)OpCode.ASSERT })
                .ToArray();
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter());

            var client = rpcClientMock.Object;
            var result = await nep17API.CreateTransferTxAsync(rpcClientMock.Object.NativeContractRepository.GAS.Hash, keyPair1, UInt160.Zero, new BigInteger(1_00000000), null, true);

            testScript = rpcClientMock.Object.NativeContractRepository.GAS.Hash.MakeScript("transfer", sender, UInt160.Zero, new BigInteger(1_00000000), string.Empty)
                .Concat(new[] { (byte)OpCode.ASSERT })
                .ToArray();
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter());

            result = await nep17API.CreateTransferTxAsync(rpcClientMock.Object.NativeContractRepository.GAS.Hash, keyPair1, UInt160.Zero, new BigInteger(1_00000000), string.Empty, true);
            Assert.IsNotNull(result);
        }
    }
}
