using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnidecodeSharpFork;

namespace Atlas
{
    namespace Utils
    {
        public static class StringExtensions
        {
            private static readonly HashSet<string> javaKeywords = new HashSet<String> { "abstract", "continue", "for", "new", "switch", "assert", "default", "goto", "package", "synchronized", "boolean", "do", "if", "private", "this", "break", "double", "implements", "protected", "throw", "byte", "else", "import", "public", "throws", "case", "enum", "instanceof", "return", "transient", "catch", "extends", "int", "short", "try", "char", "final", "interface", "static", "void", "class", "finally", "long", "strictfp", "volatile", "const", "float", "native", "super", "while" };

            private static readonly HashSet<string> csKeywords = new HashSet<String> { "abstract", "add", "as", "ascending", "async", "await", "base", "bool", "break", "by", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "descending", "do", "double", "dynamic", "else", "enum", "equals", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "from", "get", "global", "goto", "group", "if", "implicit", "in", "int", "interface", "internal", "into", "is", "join", "let", "lock", "long", "namespace", "new", "null", "object", "on", "operator", "orderby", "out", "override", "params", "partial", "private", "protected", "public", "readonly", "ref", "remove", "return", "sbyte", "sealed", "select", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "value", "var", "virtual", "void", "volatile", "where", "while", "yield" };

            public static String Capitalize(this String s)
            {
                return char.ToUpper(s[0]) + s.Substring(1);
            }

            public static string AsVarname(this string str)
            {
                string varname = "";

                str = str.Unidecode();

                // remove invalid characters
                foreach (char c in str)
                {
                    if (isAlpha(c))
                    {
                        varname += c;
                    }
                }

                // do not use keywords
                if (isKeyword(str))
                {
                    varname = Capitalize(varname);
                }

                // The variable cannot be empty
                if (varname.Length == 0)
                {
                    varname = "x";
                }

                // it cannot start with a number
                if (isDigit(varname[0]))
                {
                    varname = "_" + varname;
                }

                return varname;
            }

            private static bool isAlpha(char c)
            {
                return (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || isDigit(c) || c == '_');
            }

            private static bool isDigit(char c)
            {
                return c >= '0' && c <= '9';
            }

            private static bool isKeyword(String s)
            {
                return csKeywords.Contains(s);
            }
        }
    }
}