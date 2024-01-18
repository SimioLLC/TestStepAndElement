using SimioAPI;
using SimioAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Xml.Linq;

namespace TestStepAndElement
{
    internal class TestStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces.
        /// </summary>
        public string Name
        {
            get { return "TestStep"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.
        /// </summary>
        public string Description
        {
            get { return "Description text for the 'TestStep' step."; }
        }

        /// <summary>
        /// Property returning an icon to display for the step in the UI.
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return null; }
        }

        /// <summary>
        /// Property returning a unique static GUID for the step.
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        static readonly Guid MY_ID = new Guid("{2b2194b0-5ea7-4e63-ae52-5080c125e18b}");

        /// <summary>
        /// Property returning the number of exits out of the step. Can return either 1 or 2.
        /// </summary>
        public int NumberOfExits
        {
            get { return 1; }
        }

        /// <summary>
        /// Method called that defines the property schema for the step.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            // Example of how to add a property definition to the step.
            IPropertyDefinition pd;
            pd = schema.AddExpressionProperty("TestElementName", "ABC");
            pd.Required = true;

            IRepeatGroupPropertyDefinition elements = schema.AddRepeatGroupProperty("TestElements");
            pd = elements.PropertyDefinitions.AddElementProperty("TestElement", TestElementDefinition.MY_ID);
            pd = schema.AddStateProperty("ResponseValue");
            pd.Required = true;
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process.
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new TestStep(properties);
        }

        #endregion
    }

    internal class TestStep : IStep
    {
        IPropertyReaders _properties;
        IPropertyReader _testElementName;
        IRepeatingPropertyReader _testElements;
        IPropertyReader _responseValue;

        public TestStep(IPropertyReaders properties)
        {
            _properties = properties;
            _testElementName = (IPropertyReader)_properties.GetProperty("TestElementName");
            _testElements = (IRepeatingPropertyReader)_properties.GetProperty("TestElements");
            _responseValue = (IPropertyReader)_properties.GetProperty("ResponseValue");
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {
            var testElementExpression = (IExpressionPropertyReader)_testElementName;
            var elementName = testElementExpression.GetExpressionValue((IExecutionContext)context).ToString();

            IRepeatingPropertyReader elements = (IRepeatingPropertyReader)_properties.GetProperty("TestElements");

            int numInRepeatGroups = _testElements.GetCount(context);
            object[] paramsArray = new object[numInRepeatGroups];

            double realResponse = 0.0;

            // an array of string values from the repeat group's list of strings
            for (int i = 0; i < numInRepeatGroups; i++)
            {
                // The thing returned from GetRow is IDisposable, so we use the using() pattern here
                using (IPropertyReaders elementRow = _testElements.GetRow(i, context))
                {
                    // Get the string property
                    IElementProperty element = (IElementProperty)elementRow.GetProperty("TestElement");
                    TestElement testElement = (TestElement)element.GetElement(context);
                    if (testElement.GetName() == elementName)
                    {
                        realResponse = testElement.GetValue();
                        break;
                    }
                }
            }

            IStateProperty responseStateProp = (IStateProperty)_responseValue;
            IState responseState = responseStateProp.GetState(context);
            IRealState responseRealState = responseState as IRealState;
            responseRealState.Value = realResponse;

            // Example of how to display a trace line for the step.
            context.ExecutionInformation.TraceInformation(String.Format("The value for '{0}' is '{1}'.", elementName, realResponse.ToString()));

            return ExitType.FirstExit;
        }

        #endregion
    }
}
