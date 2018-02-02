﻿using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.SqlServer.Dac.Extensions.Prototype;
using Microsoft.SqlServer.Dac.Model;
using NUnit.Framework;
using ReverseEngineer20.ReverseEngineer;
using System.IO;
using System.Linq;

namespace UnitTests
{
    [TestFixture]
    public class DacpacTest
    {
        private TSqlTypedModel model;
        [SetUp]
        public void Setup()
        {
            model = new TSqlTypedModel("Chinook.dacpac");
        }

        [Test]
        public void CanGetTableNames()
        {
            // Arrange
            var builder = new DacpacTableListBuilder("Chinook.dacpac");

            // Act
            var result = builder.GetTableNames();

            // Assert
            Assert.AreEqual("dbo.Album", result[0]);
            Assert.AreEqual(11, result.Count);
        }

        [Test]
        public void CanEnumerateTables()
        {
            //TODO type aliases
            //TSqlUserDefinedType

            var dbModel = new DatabaseModel();
            dbModel.DatabaseName = Path.GetFileNameWithoutExtension("Chinook.dacpac");

            var tables = model.GetObjects<TSqlTable>(DacQueryScopes.UserDefined)
                .Where(t => t.PrimaryKeyConstraints.Count() > 0
                && !t.GetProperty<bool>(Table.IsAutoGeneratedHistoryTable))
                .ToList();
            
            //TODO Table filtering
            // Exclude HistoryRepository.DefaultTableName

            foreach (var item in tables)
            {
                var dbTable = new DatabaseTable
                {
                    Name = item.Name.Parts[1],
                    Schema = item.Name.Parts[0],

                };
                dbTable["SqlServer:MemoryOptimized"] = item.MemoryOptimized;

                GetColumns(item, dbTable);

                GetPrimaryKey(item, dbTable);

                //dbTable.ForeignKeys.Add( = null;
                //dbTable.UniqueConstraints = null;
                //dbTable.Indexes = null;

                dbModel.Tables.Add(dbTable);
            }

            // Assert
            Assert.AreEqual(tables.Count(), dbModel.Tables.Count());
        }

        private static void GetColumns(TSqlTable item, DatabaseTable dbTable)
        {
            var tableColumns = item.Columns
                .Where(i => !i.GetProperty<bool>(Column.IsHidden)
                && i.ColumnType != ColumnType.ColumnSet
                );

            foreach (var col in item.Columns)
            {
                var dbColumn = new DatabaseColumn
                {
                    Table = dbTable,
                    Name = col.Name.Parts[2],
                    IsNullable = col.Nullable
                };
                    
                //TSqlDefaultConstraint
                
                //dbColumn.ComputedColumnSql = null;
                //dbColumn.DefaultValueSql = null;
                
                //dbColumn.StoreType = null;
                //dbColumn.ValueGenerated = null;
                
                dbTable.Columns.Add(dbColumn);
            }
        }

        private void GetPrimaryKey(TSqlTable table, DatabaseTable dbTable)
        {
            if (table.PrimaryKeyConstraints.Count() > 0)
            {
                var pk = table.PrimaryKeyConstraints.First();
                var primaryKey = new DatabasePrimaryKey();
                primaryKey.Name = pk.Name.Parts[1];
                primaryKey.Table = dbTable;

                foreach (var pkCol in pk.Columns)
                {
                    var dbCol = dbTable.Columns
                        .Where(c => c.Name == pkCol.Name.Parts[2])
                        .Single();

                    primaryKey.Columns.Add(dbCol);
                }
            };
        }
    }
}