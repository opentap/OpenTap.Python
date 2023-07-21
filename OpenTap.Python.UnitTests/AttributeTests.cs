namespace OpenTap.Python.UnitTests
{
    public class AttributeTests
    {
        public void TestOutputAvailability()
        {
            var type = TypeData.GetTypeData("Test.ParentTest");
            var property = type.GetMember("outputProp");
            var output = property.GetAttribute<OutputAttribute>();
            Assert.AreEqual(OutputAvailability.BeforeRun, output.Availability);

            var plan = new TestPlan();
            var step = (ITestStep) type.CreateInstance();
            plan.ChildTestSteps.Add(step);
            var r = plan.Execute();
            Assert.IsTrue(r.Verdict != Verdict.Error);
        }
    }
}
