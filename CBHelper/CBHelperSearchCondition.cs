/* Copyright (C) 2012 cloudbase.io
 
 This program is free software; you can redistribute it and/or modify it under
 the terms of the GNU General Public License, version 2, as published by
 the Free Software Foundation.
 
 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 for more details.
 
 You should have received a copy of the GNU General Public License
 along with this program; see the file COPYING.  If not, write to the Free
 Software Foundation, 59 Temple Place - Suite 330, Boston, MA
 02111-1307, USA.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBHelper
{
    /// <summary>
    /// The set of possible operators for a CBHelperSearchCondition
    /// </summary>
    public enum CBConditionOperator
    {
        CBOperatorEqual = 0,
        CBOperatorLess = 1,
        CBOperatorLessOrEqual = 2,
        CBOperatorBigger = 3,
        CBOperatorBiggerOrEqual = 4,
        CBOperatorAll = 5,
        CBOperatorExists = 6,
        CBOperatorMod = 7,
        CBOperatorNe = 8,
        CBOperatorIn = 9,
        CBOperatorNin = 10,
        CBOperatorSize = 11,
        CBOperatorType = 12,
        CBOperatorWithin = 13,
        CBOperatorNear = 14
    }

    /// <summary>
    /// The possible links between two CBHelperSearchCondition objects.
    /// </summary>
    public enum CBConditionLink
    {
        CBConditionLinkAnd = 0,
        CBConditionLinkOr = 1,
        CBConditionLinkNor = 2
    }

    /// <summary>
    /// This object is used by the cloudbase APIs to run search over a collection. Each CBHelperSearchCondition object can
    /// contain a List of subconditions (other CBHelperSearchCondition objects)
    /// </summary>
    public class CBHelperSearchCondition
    {
        private List<CBHelperSearchCondition> conditions;
        private string field;
        private object value;
        private CBConditionOperator conditionOperator;
        private CBConditionLink conditionLink;
        /// <summary>
        /// The link between the previous analyzed condition and this one.
        /// </summary>
        public CBConditionLink ConditionLink
        {
            get { return conditionLink; }
            set { conditionLink = value; }
        }

        private static string[] CBConditionOperator_ToString = { 
            "",
            "$lt",
            "$lte",
            "$gt",
            "$gte",
            "$all",
            "$exists",
            "$mod",
            "$ne",
            "$in",
            "$nin",
            "$size",
            "$type",
            "$within",
            "$near"
        };
        private static string[] CBConditionLink_ToString = {
            "$and",
            "$or",
            "$nor"
        };

        /// <summary>
        /// Creates a new empty CBHelperSearchCondition object containing a number of subconditions
        /// </summary>
        /// <param name="SubConditions">A List of CBHelperSearchCondition objects</param>
        public CBHelperSearchCondition(List<CBHelperSearchCondition> SubConditions)
        {
            this.conditions = SubConditions;
        }

        /// <summary>
        /// Shortcut to initialise a simple condition object
        ///
        /// The possible operators for each condition are:
        /// CBOperatorEqual,
        /// CBOperatorLess,
        /// CBOperatorLessOrEqual,
        /// CBOperatorBigger,
        /// CBOperatorBiggerOrEqual,
        /// CBOperatorAll,
        /// CBOperatorExists,
        /// CBOperatorMod,
        /// CBOperatorNe,
        /// CBOperatorIn,
        /// CBOperatorNin,
        /// CBOperatorSize,
        /// CBOperatorType
        /// </summary>
        /// <param name="field">The name of the field to run the search over</param>
        /// <param name="op">The operator</param>
        /// <param name="value">The value to compare against</param>
        public CBHelperSearchCondition(string field, CBConditionOperator op, string value)
        {
            this.field = field;
            this.conditionOperator = op;
            this.value = value;
        }

        /// <summary>
        /// Adds a new sub-condition to the current condition using AND as a link.
        /// </summary>
        /// <param name="cond">The new sub-condition</param>
        public void AddAnd(CBHelperSearchCondition cond) {
            if (this.conditions == null)
                this.conditions = new List<CBHelperSearchCondition>();

            cond.ConditionLink = CBConditionLink.CBConditionLinkAnd;
            this.conditions.Add(cond);
        }
        /// <summary>
        /// Adds a new sub-condition to the current condition using OR as a link.
        /// </summary>
        /// <param name="cond">The new sub-condition</param>
        public void AddOr(CBHelperSearchCondition cond)
        {
            if (this.conditions == null)
                this.conditions = new List<CBHelperSearchCondition>();

            cond.ConditionLink = CBConditionLink.CBConditionLinkOr;
            this.conditions.Add(cond);
        }
        /// <summary>
        /// Adds a new sub-condition to the current condition using NOR as a link.
        /// </summary>
        /// <param name="cond">The new sub-condition</param>
        public void AddNor(CBHelperSearchCondition cond)
        {
            if (this.conditions == null)
                this.conditions = new List<CBHelperSearchCondition>();

            cond.ConditionLink = CBConditionLink.CBConditionLinkNor;
            this.conditions.Add(cond);
        }
        /// <summary>
        /// Serializes the current condition and sub-conditions to a nested set of Dictionaries which can
        /// then be serialised to JSON to be sent to the cloudbase.io APIs
        /// </summary>
        /// <returns>The Dictionary representation of the current set of conditions</returns>
        public Dictionary<string, object> SeralizeConditions()
        {
            Dictionary<string, object> conds = CBHelperSearchCondition.SerializeConditions(this);
            Dictionary<string, object> finalConditions = new Dictionary<string, object>();

            if (!conds.ContainsKey("cb_search_key"))
                finalConditions.Add("cb_search_key", conds);
            else
                finalConditions = conds;

            return finalConditions;
        }
        /// <summary>
        /// Serializes the given condition and sub-conditions to a nested set of Dictionaries which can
        /// then be serialised to JSON to be sent to the cloudbase.io APIs
        /// </summary>
        /// <returns>The Dictionary representation of the current set of conditions</returns>
        public static Dictionary<string, object> SerializeConditions(CBHelperSearchCondition cond)
        {
            Dictionary<string, object> output = new Dictionary<string,object>();
            
            if (cond.field == null) 
            {
                if (cond.conditions.Count > 1) {
                    List<object> curObject = new List<object>();
            
                    int prevLink = -1;
                    int count = 0;
                    foreach (CBHelperSearchCondition curGroup in cond.conditions)
                    {
                        if (prevLink != -1 && prevLink != (int)curGroup.ConditionLink) {
                            output.Add(CBConditionLink_ToString[prevLink], curObject);
                            curObject = new List<object>();
                        }
                        curObject.Add(CBHelperSearchCondition.SerializeConditions(curGroup));
                        prevLink = (int)curGroup.ConditionLink;
                        count++;
                        if (count == cond.conditions.Count) {
                            output.Add(CBConditionLink_ToString[prevLink], curObject);
                        }
                    }
                }
                else if (cond.conditions.Count == 1)
                {
                    output = CBHelperSearchCondition.SerializeConditions(cond.conditions[0]);
                }
            }
            else
            {
                List<object> modArray = new List<object>();
                Dictionary<string, object> newCond = new Dictionary<string, object>();

                switch (cond.conditionOperator) {
                    case CBConditionOperator.CBOperatorEqual:
                        output.Add(cond.field, cond.value);
                        break;
                    case CBConditionOperator.CBOperatorAll:
                    case CBConditionOperator.CBOperatorExists:
                    case CBConditionOperator.CBOperatorNe:
                    case CBConditionOperator.CBOperatorIn:
                    case CBConditionOperator.CBOperatorNin:
                    case CBConditionOperator.CBOperatorSize:
                    case CBConditionOperator.CBOperatorType:
                        newCond.Add(CBConditionOperator_ToString[(int)cond.conditionOperator], cond.value);
                        output.Add(cond.field, newCond);
                        break;
                    case CBConditionOperator.CBOperatorMod:
                        modArray.Add(cond.value);
                        modArray.Add(1);
                        newCond.Add(CBConditionOperator_ToString[(int)cond.conditionOperator], modArray);
                        output.Add(cond.field, newCond);
                        break;
                    default:
                        break;
                }
            }
            
            return output;
        }
    }
}
