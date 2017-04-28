﻿using EasyLOB.Data;
using EasyLOB.Persistence;
using Syncfusion.EJ.Export;
using Syncfusion.JavaScript;
using Syncfusion.JavaScript.Models;
using Syncfusion.XlsIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;

namespace EasyLOB.Library.Syncfusion
{
    public partial class SyncfusionGrid
    {
        #region Properties

        public string DataNamespace { get; }

        public bool HasServerSideJoins { get; }

        #endregion Properties

        #region Methods

        public SyncfusionGrid(Type type, ZDBMS dbms)
        {
            DataNamespace = type.Namespace;
            HasServerSideJoins = PersistenceHelper.HasServerSideJoins(dbms);
        }

        #endregion Methods

        #region Methods Export (static)

        // DataManager                      =>  GridProperties

        // SearchFilter
        // dataManager.Search[0].Fields     =>
        // dataManager.Search[0].Key        =>
        // dataManager.Search[0].Operator   =>

        // WhereFilter                          FilteredColumn
        // dataManager.Where[0].Field       =>  gridProperties.FilterSettings.FilteredColumns[0].Field
        // dataManager.Where[0].Operator    =>  gridProperties.FilterSettings.FilteredColumns[0].Operator
        // dataManager.Where[0].predicates  =>  gridProperties.FilterSettings.FilteredColumns[0].Predicate
        // dataManager.Where[0].value       =>  gridProperties.FilterSettings.FilteredColumns[0].Value

        // Sort                                 SortedColumn
        // dataManager.Sorted[0].Name       =>  gridProperties.SortSettings.SortedColumns[0].Field
        // dataManager.Sorted[0].Direction  =>  gridProperties.SortSettings.SortedColumns[0].Direction

        public static void ExportToExcel(string gridModel, IEnumerable data, string fileName, string theme)
        {
            GridProperties gridProperties = ModelToObject(gridModel);

            ExcelExport export = new ExcelExport();
            export.Export(gridProperties, data, fileName, ExcelVersion.Excel2013, false, false, theme);
        }

        public static void ExportToPdf(string gridModel, IEnumerable data, string fileName, string theme)
        {
            GridProperties gridProperties = ModelToObject(gridModel);

            PdfExport export = new PdfExport();
            export.Export(gridProperties, data, fileName, false, false, true, theme); // UNICODE = true
            //export.Export(gridProperties, data, fileName, false, false, theme);
        }

        public static void ExportToWord(string gridModel, IEnumerable data, string fileName, string theme)
        {
            GridProperties gridProperties = ModelToObject(gridModel);

            WordExport export = new WordExport();
            export.Export(gridProperties, data, fileName, false, false, theme);
        }

        public static GridProperties ModelToObject(string gridModel)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            IEnumerable data = (IEnumerable)serializer.Deserialize(gridModel, typeof(IEnumerable));
            GridProperties gridProperties = new GridProperties();
            foreach (KeyValuePair<string, object> pair in data)
            {
                var property = gridProperties.GetType().GetProperty(pair.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    System.Type type = property.PropertyType;
                    string serialize = serializer.Serialize(pair.Value);
                    object value = serializer.Deserialize(serialize, type);
                    property.SetValue(gridProperties, value, null);
                }
            }

            return gridProperties;
        }

        #endregion Methods Export (static)

        #region Methods Sycnfusion.Javascript.DataManager - Grid

        public string ToLinqWhere(List<SearchFilter> searchFilters, List<WhereFilter> whereFilters, ArrayList arguments)
        {
            int argument = 0;
            string result;

            string search = SearchToLinq(searchFilters, arguments, ref argument);
            string where = WhereToLinq(whereFilters, arguments, ref argument);
            if (!String.IsNullOrEmpty(search) && !String.IsNullOrEmpty(where))
            {
                result = search + " && " + where;
            }
            else
            {
                result = search + where;
            }

            return result;
        }

        public string ToLinqOrderBy(List<Sort> sorts)
        {
            return SortToLinq(sorts);
        }

        #endregion Methods Sycnfusion.Javascript.DataManager - Grid

        #region Methods Sycnfusion.Javascript.DataManager - Search

        private string SearchToLinq(List<SearchFilter> searchFilters, ArrayList arguments, ref int argument)
        {
            string where = "";

            if (searchFilters != null)
            {
                string condition = " || ";
                foreach (SearchFilter searchFilter in searchFilters)
                {
                    foreach (string field in searchFilter.Fields)
                    {
                        if (field.Contains(","))
                        {
                            // searchFilters[0].Fields = "[\"CategoryId\",\"CategoryName\"]"

                            string[] searchFields = field.Replace("[", "").Replace("]", "").Split(',');
                            foreach (string searchField in searchFields)
                            {
                                string linq = SearchExpressionToLinq(searchFilter.Operator,
                                    searchField.Replace("\"", ""),
                                    searchFilter.Key, arguments, ref argument);
                                if (linq != "")
                                {
                                    where = where != "" ? where + condition : where;
                                    where += linq;
                                }
                            }
                        }
                        else
                        {
                            // searchFilters[0].Fields = "[\"CategoryId\"]"

                            string linq = SearchExpressionToLinq(searchFilter.Operator,
                                field.Replace("[", "").Replace("]", "").Replace("\"", ""),
                                searchFilter.Key, arguments, ref argument);
                            if (linq != "")
                            {
                                where = !String.IsNullOrEmpty(where) ? where + condition : where;
                                where += linq;
                            }
                        }
                    }
                }

                if (where != "")
                {
                    where = "(" + where + ")";
                }
            }

            return where;
        }

        private string SearchExpressionToLinq(string whereOperator, string field, string key,
            ArrayList arguments, ref int argument)
        {
            string linq = "";

            field = LookupTextToWhere(field);
            if (field != "")
            {
                arguments.Add(key);
                linq = field + ".ToString().Contains(@" + argument++.ToString() + ")";
            }

            return linq;
        }

        #endregion Methods Sycnfusion.Javascript.DataManager - Search

        #region Methods Sycnfusion.Javascript.DataManager - Where

        private string WhereToLinq(List<WhereFilter> whereFilters, ArrayList arguments, ref int argument)
        {
            string where = "";

            if (whereFilters != null)
            {
                where = WhereFilterToLinq(whereFilters[0], arguments, ref argument);
            }

            return where;
        }

        private string WhereFilterToLinq(WhereFilter whereFilter, ArrayList arguments, ref int argument)
        {
            string where = "";

            if (!String.IsNullOrEmpty(whereFilter.Condition))
            {
                string condition = whereFilter.Condition == "and" ? " && " : " || ";

                foreach (WhereFilter predicate in whereFilter.predicates)
                {
                    string linq = WhereFilterToLinq(predicate, arguments, ref argument);
                    if (linq != "")
                    {
                        where = where != "" ? where + condition : where;
                        where += linq;
                    }
                }

                if (where != "")
                {
                    where = "(" + where + ")";
                }
            }
            else if (whereFilter.value != null)
            {
                where += WhereExpressionToLinq(whereFilter.Operator, whereFilter.Field, whereFilter.value, arguments, ref argument);
            }

            return where;
        }

        private string WhereExpressionToLinq(string whereOperator, string field, object value,
            ArrayList arguments, ref int argument)
        {
            string linq = "";

            field = LookupTextToWhere(field);
            if (field != "")
            {
                // ej.FilterOperators
                switch (whereOperator.ToLower())
                {
                    case "contains": // String
                        arguments.Add(value);
                        linq = field + ".Contains(@" + argument++.ToString() + ")";
                        break;

                    case "endswith": // String
                        arguments.Add(value);
                        linq = field + ".EndsWith(@" + argument++.ToString() + ")";
                        break;

                    case "equal":
                        arguments.Add(value);
                        linq = field + " == @" + argument++.ToString();
                        //linq = field + ".Equals(@" + argument++.ToString() + ")"; // NULLABLES do not support Equals()
                        break;

                    case "greaterthan":
                        arguments.Add(value);
                        linq = field + " > @" + argument++.ToString();
                        //linq = field + ".CompareTo(@" + argument++.ToString() + ") > 0"; // NULLABLES do not support CompareTo()
                        break;

                    case "greaterthanorequal":
                        arguments.Add(value);
                        linq = field + " >= @" + argument++.ToString();
                        //linq = field + ".CompareTo(@" + argument++.ToString() + ") >= 0"; // NULLABLES do not support CompareTo()
                        break;

                    case "lessthan":
                        arguments.Add(value);
                        linq = field + " < @" + argument++.ToString();
                        //linq = field + ".CompareTo(@" + argument++.ToString() + ") < 0"; // NULLABLES do not support CompareTo()
                        break;

                    case "lessthanorequal":
                        arguments.Add(value);
                        linq = field + " <= @" + argument++.ToString();
                        //linq = field + ".CompareTo(@" + argument++.ToString() + ") <= 0"; // NULLABLES do not support CompareTo()
                        break;

                    case "notequal":
                        arguments.Add(value);
                        linq = field + " != @" + argument++.ToString();
                        //linq = "!" + field + ".Equals(@" + argument++.ToString() + ")"; // NULLABLES do not support Equals()
                        break;

                    case "startswith": // String
                        arguments.Add(value);
                        linq = field + ".StartsWith(@" + argument++.ToString() + ")";
                        break;

                    default:
                        arguments.Add(value);
                        linq = field + " == @" + argument++.ToString();
                        break;
                }
            }

            return linq;
        }

        #endregion Methods Sycnfusion.Javascript.DataManager - Where

        #region Methods Sycnfusion.Javascript.DataManager - Sort

        private string SortToLinq(List<Sort> sorts)
        {
            string orderBy = "";

            if (sorts != null)
            {
                foreach (Sort sort in sorts)
                {
                    string linq = SortExpressionToLinq(sort.Name, sort.Direction);
                    if (linq != "")
                    {
                        orderBy = orderBy != "" ? orderBy + ", " : orderBy;
                        orderBy += linq;
                    }
                }
            }

            return orderBy;
        }

        private string SortExpressionToLinq(string field, string orderByDirection)
        {
            string direction;

            switch (orderByDirection)
            {
                case "ascending":
                    direction = "";
                    break;

                case "descending":
                    direction = "desc";
                    break;

                default:
                    direction = "";
                    break;
            }

            return LookupTextToLINQOrderBy(field, direction); ;
        }

        #endregion Methods Sycnfusion.Javascript.DataManager - Sort

        #region Methods Sycnfusion.Javascript.DataManager - LookupText

        private string LookupTextToWhere(string field)
        {
            // ClientLookupText -> Client.Name

            string result = field;

            if (field.EndsWith("LookupText"))
            {
                if (HasServerSideJoins)
                {
                    // field                    words                       entityLINQ | entity
                    //
                    // ClientLookupText         Client LookupText           Client | Client
                    // ClientAreaLookupText     Client Area LookupText      ClientArea | ClientArea
                    // Client1LookupText        Client 1 LookupText         Client1 | Client
                    // ClientArea1LookupText    Client Area 1 LookupText    ClientArea1 | ClientArea
                    //

                    field = field.Replace("LookupText", "");

                    string entityLINQ = field;

                    string[] words = CodeSmithHelper.StringSplitPascalCase(field).Split(' ');
                    string entity1 = field;
                    string entity2 = "";
                    for (int i = 0; i < words.Length - 1; i++)
                    {
                        entity2 += words[i];
                    }

                    System.Type entityType = LibraryHelper.GetType(DataNamespace + "." + entity1);
                    if (entityType == null)
                    {
                        entityType = LibraryHelper.GetType(DataNamespace + "." + entity2);
                    }

                    if (entityType == null)
                    {
                        result = "";
                    }
                    else
                    {
                        IZDataProfile dataProfile = DataHelper.GetDataProfile(entityType);
                        result = entityLINQ + "." + dataProfile.Class.Lookup;
                    }
                }
                else
                {
                    result = "";
                }
            }

            return result;
        }

        private string LookupTextToLINQOrderBy(string field, string direction)
        {
            // AlbumLookupText -> Album.Title

            string result = field + " " + direction;

            if (field.EndsWith("LookupText"))
            {
                if (HasServerSideJoins)
                {
                    string entity = field.Replace("LookupText", "");
                    System.Type entityType = LibraryHelper.GetType(DataNamespace + "." + entity);
                    IZDataProfile dataProfile = DataHelper.GetDataProfile(entityType);
                    if (dataProfile != null)
                    {
                        result = entity + "." + dataProfile.Class.LINQOrderBy + " " + direction;
                    }
                    //ZDataProfile dataProfile = DataHelper.GetDataProfile(entityType);
                    //if (dataProfile != null)
                    //{
                    //    result = entity + "." + dataProfile.Class.LINQOrderBy + " " + direction;
                    //}
                }
                else
                {
                    result = "";
                }
            }

            return result;
        }

        #endregion Methods Sycnfusion.Javascript.DataManager - LookupText
    }
}