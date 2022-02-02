using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic.Type
{
    public class VisualBasicTypeIdentifierGenerator : ITypeIdentifierGenerator
    {
        private static readonly IEnumerable<string> exemptValues = new[] { "Nullable(Of Decimal)", "Nullable(Of Integer)", "Nullable(Of Boolean)" };
        private static readonly IEnumerable<string> reservedWords = new[] { "AddHandler", "AddressOf", "Alias", "And", "AndAlso", "As", "Boolean", "ByRef", "Byte", "ByVal", "Call", "Case", "Catch", "CBool", "CByte", "CChar", "CDate", "CDbl", "CDec", "Char", "CInt", "Class", "CLng", "CObj", "Const", "Continue", "CSByte", "CShort", "CSng", "CStr", "CType", "CUInt", "CULng", "CUShort", "Date", "Decimal", "Declare", "Default", "Delegate", "Dim", "DirectCast", "Do", "Double", "Each", "Else", "ElseIf", "End", "EndIf", "Enum", "Erase", "Error", "Event", "Exit", "False", "Finally", "For", "Friend", "Function", "Get", "GetType", "GetXmlNamespace", "Global", "GoSub", "GoTo", "Handles", "If", "Implements", "Imports", "In", "Inherits", "Integer", "Interface", "Is", "IsNot", "Let", "Lib", "Like", "Long", "Loop", "Me", "Mod", "Module", "MustInherit", "MustOverride", "MyBase", "MyClass", "Namespace", "Narrowing", "New", "Next", "Not", "Nothing", "NotInheritable", "NotOverridable", "Object", "Of", "On", "Operator", "Option", "Optional", "Or", "OrElse", "Overloads", "Overridable", "Overrides", "ParamArray", "Partial", "Private", "Property", "Protected", "Public", "RaiseEvent", "ReadOnly", "ReDim", "REM", "RemoveHandler", "Resume", "Return", "SByte", "Select", "Set", "Shadows", "Shared", "Short", "Single", "Static", "Step", "Stop", "String", "Structure", "Sub", "SyncLock", "Then", "Throw", "To", "True", "Try", "TryCast", "TypeOf", "UInteger", "ULong", "UShort", "Using", "Variant", "Wend", "When", "While", "Widening", "With", "WithEvents", "WriteOnly", "Xor" };

        public string EscapeReservedWord(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (reservedWords.Contains(value, StringComparer.InvariantCultureIgnoreCase))
            {
                return $"[{value}]";
            }
            else
            {
                return value;
            }
        }

        public string GenerateIdentifier(string value, bool escapeLeadingNumber = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (exemptValues.Contains(value, StringComparer.InvariantCultureIgnoreCase))
            {
                return value;
            }

            string className = value;

            className = className.Replace("<", "LessThan");
            className = className.Replace(">", "GreaterThan");
            className = className.Replace("%", "Percent");
            className = className.Replace("£", "Pound");
            className = className.Replace("=", "Equals");
            className = className.Replace("+", "Plus");

            bool isValid = SyntaxFacts.IsValidIdentifier(className);

            List<string> chars = new List<string>();
            for (int i = 0; i < className.Length; i++)
            {
                chars.Add(className.Substring(i, 1));
            }

            // Convert "my function name" to "My Function Name"
            Regex convertToSentenceCase = new Regex("\\b[a-z]");
            MatchCollection matches = convertToSentenceCase.Matches(className);
            for (int i = 0; i < matches.Count; i++)
            {
                chars[matches[i].Index] = chars[matches[i].Index].ToString().ToUpperInvariant();
            }

            className = string.Join(string.Empty, chars);

            if (!isValid)
            {
                // File name contains invalid chars, remove them
                Regex regex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
                className = regex.Replace(className, string.Empty);

                // Class name doesn't begin with a letter, insert an underscore
                if (escapeLeadingNumber && !char.IsLetter(className, 0))
                {
                    className = className.Insert(0, "_");
                }
            }

            return className.Replace(" ", string.Empty);
        }
    }
}