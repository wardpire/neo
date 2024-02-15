// Copyright (C) 2015-2024 The Neo Project.
//
// UT_JNumber.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Numerics;

namespace Neo.Json.UnitTests
{
    enum Woo
    {
        Tom,
        Jerry,
        James
    }

    [TestClass]
    public class UT_JNumber
    {
        private JNumber maxInt;
        private JNumber minInt;
        private JNumber zero;

        [TestInitialize]
        public void SetUp()
        {
            maxInt = new JNumber(JNumber.MAX_SAFE_INTEGER);
            minInt = new JNumber(JNumber.MIN_SAFE_INTEGER);
            zero = new JNumber();
        }

        [TestMethod]
        public void TestAsBoolean()
        {
            maxInt.AsBoolean().Should().BeTrue();
            minInt.AsBoolean().Should().BeTrue();
            zero.AsBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void TestBigInteger()
        {
            ((JNumber)BigInteger.One).AsNumber().Should().Be(1);
            ((JNumber)BigInteger.Zero).AsNumber().Should().Be(0);
            ((JNumber)BigInteger.MinusOne).AsNumber().Should().Be(-1);
        }

        [TestMethod]
        public void TestNullEqual()
        {
            JNumber nullJNumber = null;

            Assert.IsFalse(maxInt.Equals(null));
            Assert.IsFalse(maxInt == null);
            Assert.IsFalse(minInt.Equals(null));
            Assert.IsFalse(minInt == null);
            Assert.IsFalse(zero == null);

            Assert.ThrowsException<NullReferenceException>(() => nullJNumber == maxInt);
            Assert.ThrowsException<NullReferenceException>(() => nullJNumber == minInt);
            Assert.ThrowsException<NullReferenceException>(() => nullJNumber == zero);
        }

        [TestMethod]
        public void TestAsString()
        {
            Action action1 = () => new JNumber(double.PositiveInfinity).AsString();
            action1.Should().Throw<ArgumentException>();

            Action action2 = () => new JNumber(double.NegativeInfinity).AsString();
            action2.Should().Throw<ArgumentException>();

            Action action3 = () => new JNumber(double.NaN).AsString();
            action3.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestGetEnum()
        {
            zero.GetEnum<Woo>().Should().Be(Woo.Tom);
            new JNumber(1).GetEnum<Woo>().Should().Be(Woo.Jerry);
            new JNumber(2).GetEnum<Woo>().Should().Be(Woo.James);
            new JNumber(3).AsEnum<Woo>().Should().Be(Woo.Tom);
            Action action = () => new JNumber(3).GetEnum<Woo>();
            action.Should().Throw<InvalidCastException>();
        }

        [TestMethod]
        public void TestEqual()
        {
            Assert.IsTrue(maxInt.Equals(JNumber.MAX_SAFE_INTEGER));
            Assert.IsTrue(maxInt == JNumber.MAX_SAFE_INTEGER);
            Assert.IsTrue(minInt.Equals(JNumber.MIN_SAFE_INTEGER));
            Assert.IsTrue(minInt == JNumber.MIN_SAFE_INTEGER);
            Assert.IsTrue(zero == new JNumber());
        }
    }
}
