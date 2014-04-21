﻿using System;
using Interop.UIAutomationCore;

namespace UIAControls
{
    /// <summary>
    ///     Base class for defining a custom schema.
    ///     Responsible for defining the minimum info for a custom schema and
    ///     registering it with UI Automation.
    ///     This class is not required by UIA and doesn't correspond to anything in UIA;
    ///     it's a personal preference about the right way to represent what is similar
    ///     between various schemas and what varies.
    /// </summary>
    public abstract class CustomPatternSchemaBase
    {
        private int _patternId;
        private int _patternAvailablePropertyId;
        private bool _registered;

        // The abstract properties define the minimal data needed to express
        // a custom pattern.

        /// <summary>
        ///     The list of properties for this pattern.
        /// </summary>
        public abstract UiaPropertyInfoHelper[] Properties { get; }

        /// <summary>
        ///     The list of methods for this pattern.
        /// </summary>
        public abstract UiaMethodInfoHelper[] Methods { get; }

        /// <summary>
        ///     The list of events for this pattern.
        /// </summary>
        public abstract UiaEventInfoHelper[] Events { get; }

        /// <summary>
        ///     The unique ID for this pattern.
        /// </summary>
        public abstract Guid PatternGuid { get; }

        /// <summary>
        ///     The interface ID for the COM interface for this pattern on the client side.
        /// </summary>
        public abstract Guid PatternClientGuid { get; }

        /// <summary>
        ///     The interface ID for the COM interface for this pattern on the provider side.
        /// </summary>
        public abstract Guid PatternProviderGuid { get; }

        /// <summary>
        ///     The programmatic name for this pattern.
        /// </summary>
        public abstract string PatternName { get; }

        /// <summary>
        ///     An object that implements IUIAutomationPatternHandler to handle
        ///     dispatching and client-pattern creation for this pattern
        /// </summary>
        public abstract IUIAutomationPatternHandler Handler { get; }

        /// <summary>
        ///     The assigned ID for this pattern.
        /// </summary>
        public int PatternId
        {
            get { return _patternId; }
        }

        /// <summary>
        ///     The assigned ID for the IsXxxxPatternAvailable property.
        /// </summary>
        public int PatternAvailablePropertyId
        {
            get { return _patternAvailablePropertyId; }
        }

        /// <summary>
        ///     Helper method to register this pattern.
        /// </summary>
        public void Register(bool makeAugmentationForWpfPeers = false)
        {
            if (!_registered)
            {
                // Get our pointer to the registrar
                IUIAutomationRegistrar registrar =
                    new CUIAutomationRegistrarClass();

                // Set up the pattern struct
                var patternInfo = new UiaPatternInfoHelper(
                    PatternGuid,
                    PatternName,
                    PatternClientGuid,
                    PatternProviderGuid,
                    Handler
                    );

                // Populate it with properties and methods
                uint index = 0;
                foreach (var propertyInfo in Properties)
                {
                    patternInfo.AddProperty(propertyInfo);
                    propertyInfo.Index = index++;
                }
                foreach (var methodInfo in Methods)
                {
                    patternInfo.AddMethod(methodInfo);
                    methodInfo.Index = index++;
                }

                // Add the events, too, although they are not indexed
                foreach (var eventInfo in Events)
                {
                    patternInfo.AddEvent(eventInfo);
                }

                // Register the pattern
                var patternData = patternInfo.Data;

                // Call register pattern
                int[] propertyIds = new int[patternData.cProperties];
                int[] eventIds = new int[patternData.cEvents];
                registrar.RegisterPattern(
                    ref patternData,
                    out _patternId,
                    out _patternAvailablePropertyId,
                    patternData.cProperties,
                    propertyIds,
                    patternData.cEvents,
                    eventIds);

                // Write the property IDs back
                for (var i = 0; i < propertyIds.Length; ++i)
                {
                    Properties[i].PropertyId = propertyIds[i];
                }
                for (var i = 0; i < eventIds.Length; ++i)
                {
                    Events[i].EventId = eventIds[i];
                }

                if (makeAugmentationForWpfPeers)
                    AutomationPeerAugmentationHelper.Register(this);

                _registered = true;
            }
        }
    }
}