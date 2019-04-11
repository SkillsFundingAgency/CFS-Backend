using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DefinitionChangesDetectionServiceTests
    {
        [TestMethod]
        public void DetectChanges_GivenNoChanges_ReturnsModelWithNoChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name"
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name"
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void DetectChanges_GivenDefinitionNameChange_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>()
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name1",
                TableDefinitions = new List<TableDefinition>()
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .DefinitionChanges
                .First()
                .Should()
                .Be(DefinitionChangeType.DefinitionName);

            changes
                .NewName
                .Should()
                .Be("name2");
        }

        [TestMethod]
        public void DetectChanges_GivenTableDefinitionNameChange_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name2", FieldDefinitions = new List<FieldDefinition>() }
                }
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>()}
                }
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
             .TableDefinitionChanges
             .First()
             .HasChanges
             .Should()
             .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .ChangeTypes
                .First()
                .Should()
                .Be(TableDefinitionChangeType.DefinitionName);

            changes
                .TableDefinitionChanges
                .First()
                .TableDefinition
                .Name
                .Should()
                .Be("name2");
        }

        [TestMethod]
        public void DetectChanges_GivenNewTableDefinitionHasNewFields_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123"},
                            new FieldDefinition{ Id = "456"}
                    }}
                }
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123"}
                    }}
                }
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .ChangeTypes
                .First()
                .Should()
                .Be(FieldDefinitionChangeType.AddedField);

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .FieldDefinition
                .Id
                .Should()
                .Be("456");
        }

        [TestMethod]
        public void DetectChanges_GivenNewTableDefinitionHasLessFields_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123"}
                    }}
                }
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123"},
                            new FieldDefinition{ Id = "456"}
                    }}
                }
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .ChangeTypes
                .First()
                .Should()
                .Be(FieldDefinitionChangeType.RemovedField);

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .FieldDefinition
                .Id
                .Should()
                .Be("456");
        }

        [TestMethod]
        public void DetectChanges_GivenNewTableDefinitionHasChangedFieldNames_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field2" }
                    }}
                }
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1" },
                    }}
                }
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .ChangeTypes
                .First()
                .Should()
                .Be(FieldDefinitionChangeType.FieldName);

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .FieldDefinition
                .Name
                .Should()
                .Be("field2");
        }

        [TestMethod]
        public void DetectChanges_GivenNewTableDefinitionHasChangedFieldAggrgableTrue_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1" , IsAggregable = true}
                    }}
                }
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1",  IsAggregable = false },
                    }}
                }
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .ChangeTypes
                .First()
                .Should()
                .Be(FieldDefinitionChangeType.IsAggregable);
        }

        [TestMethod]
        public void DetectChanges_GivenNewTableDefinitionHasChangedFieldAggrgableFalse_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1" , IsAggregable = false}
                    }}
                }
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1",  IsAggregable = true },
                    }}
                }
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .ChangeTypes
                .First()
                .Should()
                .Be(FieldDefinitionChangeType.IsNotAggregable);
        }

        [TestMethod]
        public void DetectChanges_GivenNewTableDefinitionHasMultipleChanges_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field2" , IsAggregable = false},
                            new FieldDefinition{ Id = "456", Name = "field3" , IsAggregable = false},
                            new FieldDefinition{ Id = "789", Name = "field4" , IsAggregable = false}
                    }}
                }
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1",  IsAggregable = true },
                    }}
                }
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .Should()
                .HaveCount(3);

            changes
               .TableDefinitionChanges
               .First()
               .FieldChanges
               .Count(m => m.ChangeTypes.First() == FieldDefinitionChangeType.AddedField)
               .Should()
               .Be(2);

            changes
             .TableDefinitionChanges
                 .First()
                 .FieldChanges
                 .Count(m => m.ChangeTypes.First() == FieldDefinitionChangeType.FieldName)
                 .Should()
                 .Be(1);
        }

        [TestMethod]
        public void DetectChanges_GivenNewTableDefinitionHasIdentifierTypeChange_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1" , IdentifierFieldType = IdentifierFieldType.UKPRN}
                    }}
                }
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1", IdentifierFieldType = IdentifierFieldType.URN },
                    }}
                }
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .ChangeTypes
                .First()
                .Should()
                .Be(FieldDefinitionChangeType.IdentifierType);
        }

        [TestMethod]
        public void DetectChanges_GivenNewTableDefinitionHasFieldTypeChange_ReturnsModelWithChanges()
        {
            //Arrange
            DatasetDefinition newDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1" , Type = FieldType.String}
                    }}
                }
            };

            DatasetDefinition existingDefinition = new DatasetDefinition
            {
                Name = "name2",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition{ Name = "name1", FieldDefinitions = new List<FieldDefinition>{
                            new FieldDefinition{ Id = "123", Name = "field1", Type = FieldType.Decimal },
                    }}
                }
            };

            DefinitionChangesDetectionService service = new DefinitionChangesDetectionService();

            //Act
            DatasetDefinitionChanges changes = service.DetectChanges(newDefinition, existingDefinition);

            //Assert
            changes
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .HasChanges
                .Should()
                .BeTrue();

            changes
                .TableDefinitionChanges
                .First()
                .FieldChanges
                .First()
                .ChangeTypes
                .First()
                .Should()
                .Be(FieldDefinitionChangeType.FieldType);
        }
    }
}
