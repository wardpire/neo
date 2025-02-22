// Copyright (C) 2015-2025 The Neo Project.
//
// IWalletFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Native;

namespace Neo.Wallets
{
    public interface IWalletFactory
    {
        public bool Handle(string path);

        public Wallet CreateWallet(string name, string path, string password, ProtocolSettings settings, NativeContractRepository nativeContractRepository);

        public Wallet OpenWallet(string path, string password, ProtocolSettings settings, NativeContractRepository nativeContractRepository);
    }
}
