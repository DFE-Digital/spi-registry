using System;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Application.SearchAndRetrieve;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.SearchAndRetrieve.SearchRequestValidatorTests
{
    public class WhenValidatingASearchRequest
    {
        private Mock<IRepository> _repositoryMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private SearchRequestValidator _validator;

        [SetUp]
        public void Arrange()
        {
            _repositoryMock = new Mock<IRepository>();
            _repositoryMock.Setup(r => r.GetSearchableFieldNames())
                .Returns(new[] {"Name"});

            _loggerMock = new Mock<ILoggerWrapper>();

            _validator = new SearchRequestValidator(
                _repositoryMock.Object,
                _loggerMock.Object);
        }

        [Test]
        public void ThenItShouldReturnValidResultIfRequestIsValid()
        {
            var request = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchRequestGroup
                    {
                        Filter = new[]
                        {
                            new SearchRequestFilter
                            {
                                Field = "Name",
                                Operator = DataOperator.Equals,
                                Value = "something",
                            },
                        },
                        CombinationOperator = "and",
                    },
                },
                Skip = 0,
                Take = 10,
                CombinationOperator = "and",
                PointInTime = DateTime.Now,
            };

            var actual = _validator.Validate(request);

            Assert.IsTrue(actual.IsValid);
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(101)]
        public void ThenItShouldReturnInvalidResultIfTakeIsInvalid(int take)
        {
            var request = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchRequestGroup
                    {
                        Filter = new[]
                        {
                            new SearchRequestFilter
                            {
                                Field = "Name",
                                Operator = DataOperator.Equals,
                                Value = "something",
                            },
                        },
                        CombinationOperator = "and",
                    },
                },
                Skip = 0,
                Take = take,
                CombinationOperator = "and",
                PointInTime = DateTime.Now,
            };

            var actual = _validator.Validate(request);

            Assert.IsFalse(actual.IsValid);
            Assert.AreEqual(1, actual.ValidationErrors.Length);
            Assert.AreEqual($"request has invalid Take ({request.Take}). Must between 1 and 100 inclusive", actual.ValidationErrors[0]);
        }

        [TestCase(-2)]
        [TestCase(-1)]
        public void ThenItShouldReturnInvalidResultIfSkipIsInvalid(int skip)
        {
            var request = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchRequestGroup
                    {
                        Filter = new[]
                        {
                            new SearchRequestFilter
                            {
                                Field = "Name",
                                Operator = DataOperator.Equals,
                                Value = "something",
                            },
                        },
                        CombinationOperator = "and",
                    },
                },
                Skip = skip,
                Take = 10,
                CombinationOperator = "and",
                PointInTime = DateTime.Now,
            };

            var actual = _validator.Validate(request);

            Assert.IsFalse(actual.IsValid);
            Assert.AreEqual(1, actual.ValidationErrors.Length);
            Assert.AreEqual($"request has invalid Skip ({request.Skip}). Must be 0 or greater", actual.ValidationErrors[0]);
        }

        [Test]
        public void ThenItShouldReturnInvalidResultIfInvalidCombinationOperator()
        {
            var request = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchRequestGroup
                    {
                        Filter = new[]
                        {
                            new SearchRequestFilter
                            {
                                Field = "Name",
                                Operator = DataOperator.Equals,
                                Value = "something",
                            },
                        },
                        CombinationOperator = "and",
                    },
                },
                Skip = 0,
                Take = 10,
                CombinationOperator = "both",
                PointInTime = DateTime.Now,
            };

            var actual = _validator.Validate(request);

            Assert.IsFalse(actual.IsValid);
            Assert.AreEqual(1, actual.ValidationErrors.Length);
            Assert.AreEqual($"request has invalid CombinationOperator ({request.CombinationOperator}). Valid values are and, or", actual.ValidationErrors[0]);
        }

        [Test]
        public void ThenItShouldReturnInvalidResultIfNoGroupsSpecified()
        {
            var request = new SearchRequest
            {
                Groups = new SearchRequestGroup[0],
                Skip = 0,
                Take = 10,
                CombinationOperator = "and",
                PointInTime = DateTime.Now,
            };

            var actual = _validator.Validate(request);

            Assert.IsFalse(actual.IsValid);
            Assert.AreEqual(1, actual.ValidationErrors.Length);
            Assert.AreEqual("request must have at least 1 group", actual.ValidationErrors[0]);
        }

        [Test]
        public void ThenItShouldReturnInvalidResultIfGroupHasInvalidCombinationOperator()
        {
            var request = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchRequestGroup
                    {
                        Filter = new[]
                        {
                            new SearchRequestFilter
                            {
                                Field = "Name",
                                Operator = DataOperator.Equals,
                                Value = "something",
                            },
                        },
                        CombinationOperator = "both",
                    },
                },
                Skip = 0,
                Take = 10,
                CombinationOperator = "and",
                PointInTime = DateTime.Now,
            };

            var actual = _validator.Validate(request);

            Assert.IsFalse(actual.IsValid);
            Assert.AreEqual(1, actual.ValidationErrors.Length);
            Assert.AreEqual($"group at index 0 has invalid CombinationOperator ({request.Groups[0].CombinationOperator}). Valid values are and, or", actual.ValidationErrors[0]);
        }

        [Test]
        public void ThenItShouldReturnInvalidResultIfGroupHasNoFilters()
        {
            var request = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchRequestGroup
                    {
                        Filter = new SearchRequestFilter[0],
                        CombinationOperator = "and",
                    },
                },
                Skip = 0,
                Take = 10,
                CombinationOperator = "and",
                PointInTime = DateTime.Now,
            };

            var actual = _validator.Validate(request);

            Assert.IsFalse(actual.IsValid);
            Assert.AreEqual(1, actual.ValidationErrors.Length);
            Assert.AreEqual($"group at index 0 must have at least 1 filter", actual.ValidationErrors[0]);
        }

        [Test]
        public void ThenItShouldReturnInvalidResultIfGroupFilterHadInvalidField()
        {
            _repositoryMock.Setup(r => r.GetSearchableFieldNames())
                .Returns(new[] {"Ukprn", "Urn", "LegalName"});
            var request = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchRequestGroup
                    {
                        Filter = new[]
                        {
                            new SearchRequestFilter
                            {
                                Field = "Name",
                                Operator = DataOperator.Equals,
                                Value = "something",
                            },
                        },
                        CombinationOperator = "and",
                    },
                },
                Skip = 0,
                Take = 10,
                CombinationOperator = "and",
                PointInTime = DateTime.Now,
            };

            var actual = _validator.Validate(request);

            Assert.IsFalse(actual.IsValid);
            Assert.AreEqual(1, actual.ValidationErrors.Length);
            Assert.AreEqual("filter at index 0 of group at index 0 has invalid field Name. Valid values are Ukprn, Urn and LegalName", actual.ValidationErrors[0]);
        }
    }
}