using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Results.Services
{
    [TestClass]
    public class ProviderVariationAssemblerServiceTests
    {
        [TestMethod]
        public async Task GivenProviderStatusIsClosed_AndProviderHasNoSuccesors_AndHasExistingResult_ThenHasProviderClosedIsTrue()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting { ProviderId = providerId, AllocationLineId = "alloc1", Provider = new ProviderSummary { Id = providerId } }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, ReasonEstablishmentClosed = "Test closed reason", Status = "Closed" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderClosed.Should().BeTrue();
            results.First().ProviderReasonCode.Should().Be("Test closed reason");
            results.First().UpdatedProvider.Should().NotBeNull();
            results.First().UpdatedProvider.Id.Should().Be(providerId);
            results.First().DoesProviderHaveSuccessor.Should().BeFalse();
        }

        [TestMethod]
        public async Task GivenUpdatedProvider_AndProviderAuthorityHasChanged_ThenHasProviderDataChangedIsTrue()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = providerId,
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary { Id = providerId, Authority = "authority2" }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, Authority = "authority1" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderDataChanged.Should().BeTrue();
            results.First().VariationReasons.Should().Contain(VariationReason.AuthorityFieldUpdated);
        }

        [TestMethod]
        public async Task GivenUpdatedProvider_AndProviderEstablishmentNumberHasChanged_ThenHasProviderDataChangedIsTrue()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = providerId,
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary { Id = providerId, EstablishmentNumber = "en2" }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, EstablishmentNumber = "en1" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderDataChanged.Should().BeTrue();
            results.First().VariationReasons.Should().Contain(VariationReason.EstablishmentNumberFieldUpdated);
        }

        [TestMethod]
        public async Task GivenUpdatedProvider_AndProviderDfeEstablishmentNumberHasChanged_ThenHasProviderDataChangedIsTrue()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = providerId,
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary { Id = providerId, DfeEstablishmentNumber = "den2" }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, DfeEstablishmentNumber = "den1" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderDataChanged.Should().BeTrue();
            results.First().VariationReasons.Should().Contain(VariationReason.DfeEstablishmentNumberFieldUpdated);
        }

        [TestMethod]
        public async Task GivenUpdatedProvider_AndProviderNameHasChanged_ThenHasProviderDataChangedIsTrue()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = providerId,
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary { Id = providerId, Name = "name2" }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, Name = "name1" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderDataChanged.Should().BeTrue();
            results.First().VariationReasons.Should().Contain(VariationReason.NameFieldUpdated);
        }

        [TestMethod]
        public async Task GivenUpdatedProvider_AndProviderLACodeHasChanged_ThenHasProviderDataChangedIsTrue()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = providerId,
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary { Id = providerId, LACode = "lac2" }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, LACode = "lac1" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderDataChanged.Should().BeTrue();
            results.First().VariationReasons.Should().Contain(VariationReason.LACodeFieldUpdated);
        }

        [TestMethod]
        public async Task GivenUpdatedProvider_AndProviderLegalNameHasChanged_ThenHasProviderDataChangedIsTrue()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = providerId,
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary { Id = providerId, LegalName = "ln2" }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, LegalName = "ln1" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderDataChanged.Should().BeTrue();
            results.First().VariationReasons.Should().Contain(VariationReason.LegalNameFieldUpdated);
        }

        [TestMethod]
        public async Task GivenUpdatedProvider_AndAllProviderFieldsHaveChanged_ThenHasProviderDataChangedIsTrue()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = providerId,
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = providerId,
                        Authority = "authority2",
                        EstablishmentNumber = "en2",
                        DfeEstablishmentNumber = "den2",
                        LACode = "lac2",
                        LegalName = "ln2",
                        Name = "name2"
                    }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, Authority = "authority1", EstablishmentNumber = "en1", DfeEstablishmentNumber = "den1", LACode = "lac1", LegalName = "ln1", Name = "name1" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderDataChanged.Should().BeTrue();
            results.First().VariationReasons.Should().Contain(VariationReason.AuthorityFieldUpdated);
            results.First().VariationReasons.Should().Contain(VariationReason.EstablishmentNumberFieldUpdated);
            results.First().VariationReasons.Should().Contain(VariationReason.DfeEstablishmentNumberFieldUpdated);
            results.First().VariationReasons.Should().Contain(VariationReason.NameFieldUpdated);
            results.First().VariationReasons.Should().Contain(VariationReason.LACodeFieldUpdated);
            results.First().VariationReasons.Should().Contain(VariationReason.LegalNameFieldUpdated);
        }

        [TestMethod]
        public void GivenProviderStatusIsNotClosed_AndProvidersSuccessorIsSet_ThenNonRetriableErrorThrown()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting { ProviderId = providerId, AllocationLineId = "alloc1", Provider = new ProviderSummary { Id = providerId } }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, Successor = "prov2" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            Func<Task> action = async () => await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            action.Should().ThrowExactly<NonRetriableException>();
        }

        [TestMethod]
        public async Task GivenProviderStatusIsClosed_AndProviderHasSuccesor_ThenSuccessorProviderIdReturned()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting { ProviderId = providerId, AllocationLineId = "alloc1", Provider = new ProviderSummary { Id = providerId } }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, ReasonEstablishmentClosed = "Test closed reason", Successor = "prov2", Status = "Closed" },
                new ProviderSummary { Id = "prov2" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderClosed.Should().BeTrue();
            results.First().ProviderReasonCode.Should().Be("Test closed reason");
            results.First().SuccessorProviderId.Should().Be("prov2");
            results.First().DoesProviderHaveSuccessor.Should().BeTrue();
            results.First().SuccessorProvider.Should().NotBeNull();
            results.First().SuccessorProvider.Id.Should().Be("prov2", "SuccessorProvider object set correctly");
        }

        [TestMethod]
        public async Task GivenNoExistingResult_ThenHasProviderOpenedIsTrue()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" },
                            Value = 12
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>();

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, ReasonEstablishmentOpened = "Test opened reason" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderOpened.Should().BeTrue();
            results.First().ProviderReasonCode.Should().Be("Test opened reason");
        }

        [TestMethod]
        public async Task GivenNoVariation_ThenNoVariationReturned()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = providerId,
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = providerId,
                        Authority = "authority",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GivenMultipleResultsWithNoVariations_ThenNoVariationReturned()
        {
            // Arrange
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = "prov1" },
                    SpecificationId = specificationId
                },
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = "prov2" },
                    SpecificationId = specificationId
                },
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = "prov3" },
                    SpecificationId = specificationId
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = "prov1",
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = "prov1",
                        Authority = "authority",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                },
                new PublishedProviderResultExisting
                {
                    ProviderId = "prov2",
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = "prov2",
                        Authority = "authority",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                },
                new PublishedProviderResultExisting
                {
                    ProviderId = "prov3",
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = "prov3",
                        Authority = "authority",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    } }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = "prov1", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" },
                new ProviderSummary { Id = "prov2", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" },
                new ProviderSummary { Id = "prov3", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GivenMultipleResultsWithOneVariation_ThenOneVariationReturned()
        {
            // Arrange
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = "prov1" },
                    SpecificationId = specificationId
                },
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = "prov2" },
                    SpecificationId = specificationId
                },
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = "prov3" },
                    SpecificationId = specificationId
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = "prov1",
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = "prov1",
                        Authority = "authority",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                },
                new PublishedProviderResultExisting
                {
                    ProviderId = "prov2",
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = "prov2",
                        Authority = "authority",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                },
                new PublishedProviderResultExisting
                {
                    ProviderId = "prov3",
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = "prov3",
                        Authority = "authority2",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = "prov1", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" },
                new ProviderSummary { Id = "prov2", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" },
                new ProviderSummary { Id = "prov3", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().UpdatedProvider.Id.Should().Be("prov3");
        }

        [TestMethod]
        public async Task GivenCalculationWithNoFundingAndExistingResult_ThenCalculationIsIgnoredButVariationsAreReturnedSuccessfully()
        {
            // Arrange
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = "prov1" },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>()
                    {
                        new AllocationLineResult()
                        {
                            AllocationLine = new Common.Models.Reference("a1", "Allocation Line 1"),
                            Value = null,
                        },
                         new AllocationLineResult()
                        {
                            AllocationLine = new Common.Models.Reference("a1", "Allocation Line 2"),
                            Value = null,
                        },
                          new AllocationLineResult()
                        {
                            AllocationLine = new Common.Models.Reference("a1", "Allocation Line 3"),
                            Value = null,
                        },
                    },
                },
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = "prov2" },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>()
                    {
                        new AllocationLineResult()
                        {
                            AllocationLine = new Common.Models.Reference("alloc1", "Allocation Line 1"),
                            Value = 1,
                        },
                        new AllocationLineResult()
                        {
                            AllocationLine = new Common.Models.Reference("alloc2", "Allocation Line 2"),
                            Value = null,
                        },
                        new AllocationLineResult()
                        {
                            AllocationLine = new Common.Models.Reference("alloc3", "Allocation Line 3"),
                            Value = null,
                        },
                    },
                },
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = "prov3" },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>()
                    {
                        new AllocationLineResult()
                        {
                            AllocationLine = new Common.Models.Reference("alloc1", "Allocation Line 1"),
                            Value = null,
                        },
                        new AllocationLineResult()
                        {
                            AllocationLine = new Common.Models.Reference("alloc2", "Allocation Line 2"),
                            Value = null,
                        },
                        new AllocationLineResult()
                        {
                            AllocationLine = new Common.Models.Reference("alloc3", "Allocation Line 3"),
                            Value = null,
                        },
                    },
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    ProviderId = "prov2",
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = "prov2",
                        Authority = "authority",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                },
                new PublishedProviderResultExisting
                {
                    ProviderId = "prov3",
                    AllocationLineId = "alloc1",
                    Provider = new ProviderSummary
                    {
                        Id = "prov3",
                        Authority = "authority2",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                },
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = "prov1", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" },
                new ProviderSummary { Id = "prov2", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" },
                new ProviderSummary { Id = "prov3", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().UpdatedProvider.Id.Should().Be("prov3");
        }

        [TestMethod]
        public async Task GivenMultipleResultsAllWithVariations_ThenMultipleVariationReturned()
        {
            // Arrange
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult { Provider = new ProviderSummary { Id = "prov1" }, SpecificationId = specificationId },
                new ProviderResult { Provider = new ProviderSummary { Id = "prov2" }, SpecificationId = specificationId },
                new ProviderResult { Provider = new ProviderSummary { Id = "prov3" }, SpecificationId = specificationId }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    ProviderId = "prov1",
                    Provider = new ProviderSummary
                    {
                        Id = "prov1",
                        Authority = "authority2",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    ProviderId = "prov2",
                    Provider = new ProviderSummary
                    {
                        Id = "prov2",
                        Authority = "authority2",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    ProviderId = "prov3",
                    Provider = new ProviderSummary
                    {
                        Id = "prov3",
                        Authority = "authority2",
                        DfeEstablishmentNumber = "den",
                        EstablishmentNumber = "en",
                        LACode = "lac",
                        LegalName = "ln",
                        Name = "name",
                        Status = "Open"
                    }
                }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = "prov1", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" },
                new ProviderSummary { Id = "prov2", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" },
                new ProviderSummary { Id = "prov3", Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(3);
            results.Should().Contain(r => r.UpdatedProvider.Id == "prov1");
            results.Should().Contain(r => r.UpdatedProvider.Id == "prov2");
            results.Should().Contain(r => r.UpdatedProvider.Id == "prov3");
        }

        [TestMethod]
        public async Task GivenMultipleResultsForSameProviderWithVariation_ThenOneVariationReturned()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId
                },
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId
                },
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting { ProviderId = providerId, AllocationLineId = "alloc1", Provider = new ProviderSummary { Id = providerId }, }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, Authority = "authority", DfeEstablishmentNumber = "den", EstablishmentNumber = "en", LACode = "lac", LegalName = "ln", Name = "name", Status = "Open" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
        }

        [TestMethod]
        public void GivenProviderNotFoundInCoreData_ThenExceptionThrown()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting { ProviderId = providerId, AllocationLineId = "alloc1", Provider = new ProviderSummary { Id = providerId } }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>()
            {
                new ProviderSummary { Id = "prov2"}
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            Func<Task> action = async () => await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            action
                .Should()
                .ThrowExactly<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be($"Could not find provider in core data with id '{providerId}'");
        }

        [TestMethod]
        public void GivenNoCoreProviderData_ThenExceptionThrown()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary
                    {
                        Id = providerId,
                        Authority = "authority2",
                        EstablishmentNumber = "en2",
                        DfeEstablishmentNumber = "den2",
                        LACode = "lac2",
                        LegalName = "ln2",
                        Name = "name2"
                    },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting { ProviderId = providerId, AllocationLineId = "alloc1" }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>();

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            Func<Task> action = async () => await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            action
                .Should()
                .ThrowExactly<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be("Failed to retrieve core provider data");
        }

        [TestMethod]
        public async Task GivenNoExistingResultBecauseExcluded_ThenNoVariationReturned()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" },
                            Value = null // excluded
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>();

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, ReasonEstablishmentOpened = "Test opened reason" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().BeEmpty("Excluded results should not cause an open variation to be produced");
        }

        [TestMethod]
        public async Task GivenProviderHasExistingResultsForMultipleAllocationsLines_ThenOnlyOneVariationIsReturned()
        {
            // Arrange
            string providerId = "prov1";
            string specificationId = "spec123";

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc1", Name = "Allocation 1" }
                        }
                    }
                },
                new ProviderResult
                {
                    Provider = new ProviderSummary { Id = providerId },
                    SpecificationId = specificationId,
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Common.Models.Reference { Id = "alloc2", Name = "Allocation 2" }
                        }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingPublishedResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting { ProviderId = providerId, AllocationLineId = "alloc1", Provider = new ProviderSummary { Id = providerId } },
                new PublishedProviderResultExisting { ProviderId = providerId, AllocationLineId = "alloc2", Provider = new ProviderSummary { Id = providerId} }
            };

            List<ProviderSummary> coreProviderData = new List<ProviderSummary>
            {
                new ProviderSummary { Id = providerId, ReasonEstablishmentClosed = "Test closed reason", Status = "Closed" }
            };

            IProviderService providerService = CreateProviderService();
            providerService
                .FetchCoreProviderData()
                .Returns(coreProviderData);

            IProviderVariationAssemblerService service = CreateService(providerService);

            // Act
            IEnumerable<ProviderChangeItem> results = await service.AssembleProviderVariationItems(providerResults, existingPublishedResults, specificationId);

            // Assert
            results.Should().HaveCount(1);
            results.First().HasProviderClosed.Should().BeTrue();
            results.First().ProviderReasonCode.Should().Be("Test closed reason");
            results.First().UpdatedProvider.Should().NotBeNull();
            results.First().UpdatedProvider.Id.Should().Be(providerId);
            results.First().DoesProviderHaveSuccessor.Should().BeFalse();
        }

        private IProviderVariationAssemblerService CreateService(IProviderService providerService = null)
        {
            return new ProviderVariationAssemblerService(providerService ?? CreateProviderService());
        }

        private IProviderService CreateProviderService()
        {
            return Substitute.For<IProviderService>();
        }
    }
}
