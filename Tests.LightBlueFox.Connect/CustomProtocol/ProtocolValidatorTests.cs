using LightBlueFox.Connect.CustomProtocol.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Connect.CustomProtocol
{
    [TestClass]
    public class ProtocolValidatorTests
    {
        [TestMethod]
        public void TestBaseValidation()
        {
            ProtocolDefinition def1 = new ProtocolDefinition(new(), typeof(TestMessages.TestMessage1), typeof(TestMessages.TestMessage2));

            ProtocolDefinition def2 = new ProtocolDefinition(new(), typeof(TestMessages.TestMessage2), typeof(TestMessages.TestMessage1));

            Assert.IsTrue(def1.Validator.ValidateChallenge(def2.Validator.GetChallengeBytes()));
            Assert.IsTrue(def2.Validator.ValidateChallenge(def1.Validator.GetChallengeBytes()));
            Assert.IsTrue(def1.Validator.ValidateAnswer(def2.Validator.GetAnswerBytes()));
            Assert.IsTrue(def2.Validator.ValidateAnswer(def1.Validator.GetAnswerBytes()));
        }

        [TestMethod]
        public void TestInvalidValidation()
        {
            ProtocolDefinition def1 = new ProtocolDefinition(new(), typeof(TestMessages.TestMessage1), typeof(TestMessages.TestMessage2));

            ProtocolDefinition def2 = new ProtocolDefinition(new(), typeof(TestMessages.TestMessage2));

            Assert.IsTrue(!def1.Validator.ValidateChallenge(def2.Validator.GetChallengeBytes()));
            Assert.IsTrue(!def2.Validator.ValidateChallenge(def1.Validator.GetChallengeBytes()));
            Assert.IsTrue(!def1.Validator.ValidateAnswer(def2.Validator.GetAnswerBytes()));
            Assert.IsTrue(!def2.Validator.ValidateAnswer(def1.Validator.GetAnswerBytes()));
        }

        
    }
}
