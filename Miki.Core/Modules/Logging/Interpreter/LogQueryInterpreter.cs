using IA.SDK.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki.Modules.Logging.Interpreter
{
    public class LogQueryInterpreter
    {
        static List<RegexToken> tokens = new List<RegexToken>();

        static LogQueryInterpreter instance = new LogQueryInterpreter();

        public enum QueryTokenType
        {
            // command
            ADD,
            REMOVE,
            
            // types
            WELCOME,
            LEAVE,

            // Data
            STRING,
            NUMBER
        };

        public class TokenMatch
        {
            public string remainingText;
            public QueryTokenType type;
            public string value;
        }

        public class Token
        {
            public QueryTokenType type;
            public string value;
        }

        class RegexToken
        {
            string query = "";
            QueryTokenType type;

            public RegexToken(string query, QueryTokenType type)
            {
                this.query = query;
                this.type = type;

                tokens.Add(this);
            }

            public TokenMatch Match(string text)
            {
                Match m = Regex.Match(text, query);
                if(m.Success)
                {
                    return new TokenMatch()
                    {
                        remainingText = text.Substring(m.Length),
                        type = this.type,
                        value = m.Value
                    };
                }
                return null;
            }
        }

        public LogQueryInterpreter()
        {
            new RegexToken("^(new|add)", QueryTokenType.ADD);
            new RegexToken("^(remove|delete|del)", QueryTokenType.REMOVE);
            new RegexToken("^(welcome|join)", QueryTokenType.WELCOME);
            new RegexToken("^(leave)", QueryTokenType.LEAVE);
            new RegexToken("^\".*\"", QueryTokenType.STRING);
        }

        public static void Run(EventContext x)
        {
            List<Token> allTokens = instance.Tokenize(x.arguments);
            

            
        }

        public class Executor
        {
            List<Token> currentTokens = new List<Token>();

            int currentIndex = 0;

            Token Current => currentTokens[currentIndex];

            public Executor(List<Token> t)
            {
                currentTokens = t;
            }

            public void Parse()
            {
                
            }

            public bool Accept(QueryTokenType t)
            {
                if(Current.type == t)
                {
                    Next();
                    return true;
                }
                return false;
            }

            public void Next()
            {
                currentIndex++;
            }
        }

        public List<Token> Tokenize(string text)
        {
            string currentText = text;
            List<Token> allTokens = new List<Token>();

            while (!string.IsNullOrWhiteSpace(currentText))
            {
                TokenMatch m = GetMatch(currentText);
                if(m != null)
                {
                    currentText = m.remainingText;

                    allTokens.Add(new Token()
                    {
                        type = m.type,
                        value = m.value
                    });
                }
                else
                {
                    currentText = currentText.TrimStart(' ', '\n', '\r');
                }
            }
            return allTokens;
        }

        public TokenMatch GetMatch(string text)
        {
            foreach (RegexToken t in tokens)
            {
                TokenMatch m = t.Match(text);
                if (m != null)
                {
                    return m;
                }
            }
            return null;
        }
    }
}
