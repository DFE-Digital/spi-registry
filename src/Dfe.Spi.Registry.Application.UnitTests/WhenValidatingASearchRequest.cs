using System;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain.Search;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests
{
    public class WhenValidatingASearchRequest
    {
        [Test]
        public void ThenItShouldReturnNoValidationFailuresIfRequestIsValid()
        {
            var request = BuildValidRequest();

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(0, actual.Errors.Length);
        }
        
        [Test]
        public void ThenItShouldThrowAnArgumentNullExceptionIfRequestIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => SearchRequestValidator.Validate(null));
        }

        [TestCase(null)]
        [TestCase(0)]
        public void ThenItShouldReturnValidationFailureIfRequestHasNoGroups(int? count)
        {
            var request = BuildValidRequest();
            request.Groups = count.HasValue ? new SearchGroup[count.Value] : null;

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(1, actual.Errors.Length);
            Assert.AreEqual("request must have at least 1 group", actual.Errors[0]);
        }
        
        [TestCase(null)]
        [TestCase("")]
        [TestCase("nope")]
        public void ThenItShouldReturnValidationFailureIfRequestHasInvalidCominationOperator(string combinationOperator)
        {
            var request = BuildValidRequest();
            request.CombinationOperator = combinationOperator;

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(1, actual.Errors.Length);
            Assert.AreEqual($"request has invalid CombinationOperator ({combinationOperator}). Valid values are and, or", actual.Errors[0]);
        }
        
        [Test]
        public void ThenItShouldReturnValidationFailureIfRequestHasInvalidSkip()
        {
            var request = BuildValidRequest();
            request.Skip = -1;

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(1, actual.Errors.Length);
            Assert.AreEqual($"request has invalid Skip (-1). Must be 0 or greater", actual.Errors[0]);
        }
        
        [TestCase(0)]
        [TestCase(101)]
        public void ThenItShouldReturnValidationFailureIfRequestHasInvalidTake(int take)
        {
            var request = BuildValidRequest();
            request.Take = take;

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(1, actual.Errors.Length);
            Assert.AreEqual($"request has invalid Take ({take}). Must between 1 and 100 inclusive", actual.Errors[0]);
        }
        
        

        [Test]
        public void ThenItShouldReturnValidationFailureIfRequestHasGroupsWithNoCriteria()
        {
            var request = BuildValidRequest();
            request.Groups = new[]
            {
                BuildSearchGroup(null, "and"),
                BuildValidSearchGroup(),
                BuildSearchGroup(new DataFilter[0], "and"),
            };

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(2, actual.Errors.Length);
            Assert.AreEqual("group 0 must have at least 1 filter", actual.Errors[0]);
            Assert.AreEqual("group 2 must have at least 1 filter", actual.Errors[1]);
        }

        [Test]
        public void ThenItShouldReturnValidationFailureIfRequestHasGroupsWithInvalidCominationOperator()
        {
            var request = BuildValidRequest();
            request.Groups = new[]
            {
                BuildValidSearchGroup(""),
                BuildValidSearchGroup(),
                BuildValidSearchGroup(null),
                BuildValidSearchGroup("nope"),
            };

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(3, actual.Errors.Length);
            Assert.AreEqual("group 0 has invalid CombinationOperator (). Valid values are and, or", actual.Errors[0]);
            Assert.AreEqual("group 2 has invalid CombinationOperator (). Valid values are and, or", actual.Errors[1]);
            Assert.AreEqual("group 3 has invalid CombinationOperator (nope). Valid values are and, or", actual.Errors[2]);
        }
        
        

        [TestCase(null)]
        [TestCase("")]
        public void ThenItShouldReturnValidationFailureIfRequestHasFilterWithNoField(string fieldName)
        {
            var request = BuildValidRequest();
            request.Groups[0].Filter[0].Field = fieldName;

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(1, actual.Errors.Length);
            Assert.AreEqual("group 0, filter 0 must specify Field", actual.Errors[0]);
        }
        
        [Test]
        public void ThenItShouldReturnValidationFailureIfRequestHasFilterWithInvalidField()
        {
            var request = BuildValidRequest();
            request.Groups[0].Filter[0].Field = "NoSuchField";

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(1, actual.Errors.Length);
            Assert.IsTrue(actual.Errors[0].StartsWith("group 0, filter 0 has invalid Field (NoSuchField). Valid values are"));
        }
        
        [TestCase("Type", DataOperator.Contains)]
        public void ThenItShouldReturnValidationFailureIfRequestHasFilterWithInvalidOperatorForField(string fieldName, DataOperator @operator)
        {
            var request = BuildValidRequest();
            request.Groups[0].Filter[0].Field = fieldName;
            request.Groups[0].Filter[0].Operator = @operator;

            var actual = request.Validate();
            
            Assert.IsNotNull(actual);
            Assert.IsFalse(actual.IsValid);
            Assert.IsNotNull(actual.Errors);
            Assert.AreEqual(1, actual.Errors.Length);
            Assert.AreEqual($"group 0, filter 0 has invalid Operator ({@operator}) for field {fieldName}", actual.Errors[0]);
        }



        private SearchRequest BuildValidRequest()
        {
            var request = new SearchRequest
            {
                Groups = new[]
                {
                    BuildValidSearchGroup(),
                },
                CombinationOperator = "and",
                Skip = 0,
                Take = 15,
            };
            return request;
        }

        private SearchGroup BuildValidSearchGroup(string combinationOperator = "and")
        {
            var filters = new[]
            {
                new DataFilter
                {
                    Field = "Name",
                    Operator = DataOperator.Equals,
                    Value = "some-value",
                },
            };
            return BuildSearchGroup(filters, combinationOperator);
        }
        private SearchGroup BuildSearchGroup(DataFilter[] filters, string combinationOperator)
        {
            return new SearchGroup
            {
                Filter = filters,
                CombinationOperator = combinationOperator,
            };
        }
    }
}