﻿using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Interop.UIAutomationCore;
using ManagedUiaCustomizationCore;
using NSubstitute;
using NUnit.Framework;

namespace UiaControlsTest
{
    [TestFixture]
    public class AttributeDrivenPatternSchemaTests
    {
        private const string TestPatternProviderComGuid = "965D7E12-F5F5-42F9-9D72-75AAA7AE8FFD";
        private const string TestPatternClientComGuid = "267D23B7-6B12-4679-ACF0-E8FA0FB3BDD7";
        private const string TestPatternGuid = "E69F099B-7519-4CE7-9D61-77146DCB1B4A";
        private const string TestPatternBoolPropertyGuid = "DD339FFB-E244-41A2-A8A2-787F722C582B";
        private const string TestPatternIntPropertyGuid = "C6981328-E9B5-4EA1-AB21-A483D50D95BF";

        private static class Provider
        {
            private static string _dummyString;
            private static readonly IAttrDrivenTestProvider _dummyProvider = null;
            public static readonly PropertyInfo BoolPropertyProperty = TypeMember<IAttrDrivenTestProvider>.PropertyInfo(p => p.BoolProperty);
            public static readonly PropertyInfo IntPropertyProperty = TypeMember<IAttrDrivenTestProvider>.PropertyInfo(p => p.IntProperty);
            public static readonly MethodInfo VoidParameterlessMethod = ReflectionUtils.GetMethodInfo(() => _dummyProvider.VoidParameterlessMethod());
            public static readonly MethodInfo BoolParameterlessMethodWithDoSetFocus = ReflectionUtils.GetMethodInfo(() => _dummyProvider.BoolParameterlessMethodWithDoSetFocus());
            public static readonly MethodInfo IntMethodWithDoubleParam = ReflectionUtils.GetMethodInfo(() => _dummyProvider.IntMethodWithDoubleParam(0));
            public static readonly MethodInfo BoolMethodWithInAndOutParams = ReflectionUtils.GetMethodInfo(() => _dummyProvider.BoolMethodWithInAndOutParams(0, out _dummyString));
        }

        [Guid(TestPatternProviderComGuid)]
        [PatternGuid(TestPatternGuid)]
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAttrDrivenTestProvider
        {
            [PatternProperty(TestPatternBoolPropertyGuid)]
            bool BoolProperty { get; }

            [PatternProperty(TestPatternIntPropertyGuid)]
            int IntProperty { get; }

            [PatternMethod]
            void VoidParameterlessMethod();

            [PatternMethod(DoSetFocus = true)]
            bool BoolParameterlessMethodWithDoSetFocus();

            [PatternMethod]
            int IntMethodWithDoubleParam(double doubleIn);

            [PatternMethod]
            bool BoolMethodWithInAndOutParams(int intIn, out string stringOut);
        }

        [Guid(TestPatternClientComGuid)]
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAttrDrivenTestPattern
        {
            bool CurrentBoolProperty { get; }
            bool CachedBoolProperty { get; }
            int CurrentIntProperty { get; }
            int CachedIntProperty { get; }

            void VoidParameterlessMethod();
            bool BoolParameterlessMethodWithDoSetFocus();
            int IntMethodWithDoubleParam(double doubleIn);
            bool BoolMethodWithInAndOutParams(int intIn, out string stringOut);
        }

        [Test]
        public void AttributeDrivenPatternSchema_AssertGuidsAreReflectedCorrectly()
        {
            var schema = new AttributeDrivenPatternSchema(typeof(IAttrDrivenTestProvider), typeof(IAttrDrivenTestPattern));
            Assert.AreEqual(new Guid(TestPatternProviderComGuid), schema.PatternProviderGuid);
            Assert.AreEqual(new Guid(TestPatternClientComGuid), schema.PatternClientGuid);
            Assert.AreEqual(new Guid(TestPatternGuid), schema.PatternGuid);
        }

        [Test]
        public void AttributeDrivenPatternSchema_AssertRegistrationGoesSmoothly()
        {
            var schema = new AttributeDrivenPatternSchema(typeof(IAttrDrivenTestProvider), typeof(IAttrDrivenTestPattern));
            schema.Register();
        }

        [Test]
        public void AttributeDrivenPatternSchema_PropertiesAndMethodsAreMappedCorrectly()
        {
            var schema = new AttributeDrivenPatternSchema(typeof(IAttrDrivenTestProvider), typeof(IAttrDrivenTestPattern));

            var props = schema.Properties;
            Assert.AreEqual(2, props.Length);
            AssertPropertyInfo("BoolProperty", UIAutomationType.UIAutomationType_Bool, TestPatternBoolPropertyGuid, props[0]);
            AssertPropertyInfo("IntProperty", UIAutomationType.UIAutomationType_Int, TestPatternIntPropertyGuid, props[1]);

            Assert.AreEqual(4, schema.Methods.Length);

            var voidParamlessMethod = schema.Methods.Single(m => m.Data.pProgrammaticName == Provider.VoidParameterlessMethod.Name);
            Assert.AreEqual(0, voidParamlessMethod.Data.cInParameters);
            Assert.AreEqual(0, voidParamlessMethod.Data.cOutParameters);

            var boolParamlessMethodWithSetFocus = schema.Methods.Single(m => m.Data.pProgrammaticName == Provider.BoolParameterlessMethodWithDoSetFocus.Name);
            Assert.AreEqual(1, boolParamlessMethodWithSetFocus.Data.doSetFocus);
            Assert.AreEqual(0, boolParamlessMethodWithSetFocus.Data.cInParameters);
            Assert.AreEqual(1, boolParamlessMethodWithSetFocus.Data.cOutParameters);
            Assert.AreEqual(UIAutomationType.UIAutomationType_OutBool, boolParamlessMethodWithSetFocus.OutParamTypes[0]);

            var boolMethodWithInOutParams = schema.Methods.Single(m => m.Data.pProgrammaticName == Provider.BoolMethodWithInAndOutParams.Name);
            Assert.AreEqual(1, boolMethodWithInOutParams.Data.cInParameters);
            Assert.AreEqual(2, boolMethodWithInOutParams.Data.cOutParameters);
            Assert.AreEqual(UIAutomationType.UIAutomationType_Int, boolMethodWithInOutParams.InParamTypes[0]);
            Assert.AreEqual(UIAutomationType.UIAutomationType_OutString, boolMethodWithInOutParams.OutParamTypes[0]);
            Assert.AreEqual(UIAutomationType.UIAutomationType_OutBool, boolMethodWithInOutParams.OutParamTypes[1]);

            var intMethodWithDoubleParam = schema.Methods.Single(m => m.Data.pProgrammaticName == Provider.IntMethodWithDoubleParam.Name);
            Assert.AreEqual(1, intMethodWithDoubleParam.Data.cInParameters);
            Assert.AreEqual(1, intMethodWithDoubleParam.Data.cOutParameters);
            Assert.AreEqual(UIAutomationType.UIAutomationType_Double, intMethodWithDoubleParam.InParamTypes[0]);
            Assert.AreEqual(UIAutomationType.UIAutomationType_OutInt, intMethodWithDoubleParam.OutParamTypes[0]);
        }

        private void AssertPropertyInfo(string programmaticName, UIAutomationType uiaType, string guid, UiaPropertyInfoHelper propInfo)
        {
            var data = propInfo.Data;
            Assert.AreEqual(programmaticName, data.pProgrammaticName);
            Assert.AreEqual(uiaType, data.type);
            Assert.AreEqual(new Guid(guid), data.guid);
        }

        [Test]
        public void PatternHandler_DispatchesPropertiesCorrectly()
        {
            var schema = new AttributeDrivenPatternSchema(typeof(IAttrDrivenTestProvider), typeof(IAttrDrivenTestPattern));
            schema.Register();

            var paramHelper = new UiaParameterHelper(UIAutomationType.UIAutomationType_OutBool);
            var pParams = new[] {paramHelper.ToUiaParam()};

            var p = Substitute.For<IAttrDrivenTestProvider>();
            p.BoolProperty.Returns(true);
            schema.Handler.Dispatch(p, schema.Properties[0].Index, pParams, 1);
            Assert.AreEqual(true, paramHelper.Value);

            p.BoolProperty.Returns(false);
            schema.Handler.Dispatch(p, schema.Properties[0].Index, pParams, 1);
            Assert.AreEqual(false, paramHelper.Value);
        }

        [Test]
        public void UiaMethodInfoHelper_AddingInputParamAfterOutputOne_ThrowsAnException()
        {
            var inParam = new UiaParameterDescription("inInt", UIAutomationType.UIAutomationType_Int);
            var outParam = new UiaParameterDescription("outString", UIAutomationType.UIAutomationType_OutString);
            Assert.Throws<ArgumentException>(() => new UiaMethodInfoHelper("name", false, new[] {outParam, inParam}));
        }

        [Test]
        public void PatternHandler_VoidParameterlessMethodCalled_DispatchedCorrectly()
        {
            var schema = new AttributeDrivenPatternSchema(typeof(IAttrDrivenTestProvider), typeof(IAttrDrivenTestPattern));
            schema.Register();

            var pParams = new UIAutomationParameter[0];

            var p = Substitute.For<IAttrDrivenTestProvider>();
            schema.Handler.Dispatch(p, schema.Methods[0].Index, pParams, 0);
            p.Received().VoidParameterlessMethod();
        }
    }
}
