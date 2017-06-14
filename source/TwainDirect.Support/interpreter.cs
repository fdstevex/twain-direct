﻿///////////////////////////////////////////////////////////////////////////////////////
//
// TwainDirect.Support.Interpreter
//
// A simple interpreter for console mode stuff...
//
///////////////////////////////////////////////////////////////////////////////////////
//  Author          Date            Comment
//  M.McLaughlin    12-Jun-2017     Initial Release
///////////////////////////////////////////////////////////////////////////////////////
//  Copyright (C) 2017-2017 Kodak Alaris Inc.
//
//  Permission is hereby granted, free of charge, to any person obtaining a
//  copy of this software and associated documentation files (the "Software"),
//  to deal in the Software without restriction, including without limitation
//  the rights to use, copy, modify, merge, publish, distribute, sublicense,
//  and/or sell copies of the Software, and to permit persons to whom the
//  Software is furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
//  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
//  DEALINGS IN THE SOFTWARE.
///////////////////////////////////////////////////////////////////////////////////////

// Helpers...
using System;
using System.Collections.Generic;

namespace TwainDirect.Support
{


    /// <summary>
    /// Interpret and dispatch user commands...
    /// </summary>
    public class Interpreter
    {
        // Public Methods
        #region Public Methods

        /// <summary>
        /// Our constructor...
        /// </summary>
        /// <param name="a_szPrompt">initialize the prompt</param>
        public Interpreter(string a_szPrompt)
        {
            // Our prompt...
            m_szPrompt = (string.IsNullOrEmpty(a_szPrompt) ? ">>>" : a_szPrompt);
        }

        /// <summary>
        /// Prompt for input, returning a string, if there's any data...
        /// </summary>
        /// <returns>data captured</returns>
        public string Prompt()
        {
            string szCmd;

            // Read in a line...
            while (true)
            {
                // Write out the prompt...
                Console.Out.Write(m_szPrompt);

                // Read in a line...
                szCmd = Console.In.ReadLine();
                if (string.IsNullOrEmpty(szCmd))
                {
                    continue;
                }

                // Trim whitespace...
                szCmd = szCmd.Trim();
                if (string.IsNullOrEmpty(szCmd))
                {
                    continue;
                }

                // We must have data...
                break;
            }

            // All done...
            return (szCmd);
        }

        /// <summary>
        /// Tokenize a string, with support for single quotes and double quotes.
        /// Inside the body of a quote the only thing can can be (or needs to be)
        /// escaped is the current quote token.  The result is an array of strings...
        /// </summary>
        /// <param name="a_szCmd">command to tokenize</param>
        /// <returns>array of strings</returns>
        public string[] Tokenize(string a_szCmd)
        {
            int cc;
            int tt;
            char szQuote;
            string[] aszTokens;

            // We're coming out of this with at least one token...
            aszTokens = new string[1];
            tt = 0;

            // Validate...
            if (string.IsNullOrEmpty(a_szCmd))
            {
                aszTokens[tt] = "";
                return (aszTokens);
            }

            // If we have no special characters, then we're done...
            if (a_szCmd.IndexOfAny(new char[] { ' ', '\t', '\'', '"' }) == -1)
            {
                aszTokens[tt] = a_szCmd;
                return (aszTokens);
            }

            // Devour leading whitespace...
            cc = 0;
            while ((cc < a_szCmd.Length) && ((a_szCmd[cc] == ' ') || (a_szCmd[cc] == '\t')))
            {
                cc += 1;
            }

            // Loopy...
            while (cc < a_szCmd.Length)
            {
                // Handle single and double quotes...
                if ((a_szCmd[cc] == '\'') || (a_szCmd[cc] == '"'))
                {
                    // Skip the quote...
                    szQuote = a_szCmd[cc];
                    cc += 1;

                    // Copy all of the string to the next unescaped single quote...
                    while (cc < a_szCmd.Length)
                    {
                        // We found our terminator (don't copy it)...
                        if (a_szCmd[cc] == szQuote)
                        {
                            cc += 1;
                            break;
                        }

                        // We're escaping the quote...
                        if ((cc + 1 < a_szCmd.Length) && (a_szCmd[cc] == '\\') && (a_szCmd[cc + 1] == szQuote))
                        {
                            aszTokens[tt] += szQuote;
                            cc += 1;
                        }

                        // Otherwise, just copy the character...
                        else
                        {
                            aszTokens[tt] += a_szCmd[cc];
                        }

                        // Next character...
                        cc += 1;
                    }
                }

                // Handle whitespace...
                else if ((a_szCmd[cc] == ' ') || (a_szCmd[cc] == '\t'))
                {
                    // Devour all of the whitespace...
                    while ((cc < a_szCmd.Length) && ((a_szCmd[cc] == ' ') || (a_szCmd[cc] == '\t')))
                    {
                        cc += 1;
                    }

                    // If we have more data, prep for it...
                    if (cc < a_szCmd.Length)
                    {
                        string[] asz = new string[aszTokens.Length + 1];
                        Array.Copy(aszTokens, asz, aszTokens.Length);
                        asz[aszTokens.Length] = "";
                        aszTokens = asz;
                        tt += 1;
                    }
                }

                // Anything else is data in the current token...
                else
                {
                    aszTokens[tt] += a_szCmd[cc];
                    cc += 1;
                }

                // Next character.,
            }

            // All done...
            return (aszTokens);
        }

        /// <summary>
        /// Dispatch a command...
        /// </summary>
        /// <param name="a_szCmd">tokenized command</param>
        /// <param name="a_dispatchtable">dispatch table</param>
        /// <returns>true if the program should exit</returns>
        public bool Dispatch(string[] a_aszCmd, List<DispatchTable> a_ldispatchtable)
        {
            string szCmd;

            // The command to find...
            if ((a_aszCmd == null) || (a_aszCmd.Length == 0) || string.IsNullOrEmpty(a_aszCmd[0]))
            {
                Console.Out.WriteLine("dispatch error...");
                return (true);
            }

            // Find the command...
            szCmd = a_aszCmd[0].ToLowerInvariant();
            foreach (DispatchTable dispatchtable in a_ldispatchtable)
            {
                foreach (string sz in dispatchtable.m_aszCmd)
                {
                    if (sz == szCmd)
                    {
                        return (dispatchtable.m_function(a_aszCmd));
                    }
                }
            }

            // No joy...
            Console.Out.WriteLine("command not found: " + a_aszCmd[0]);
            return (false);
        }

        #endregion


        // Public Definitions
        #region Public Definitions

        /// <summary>
        /// Function to call from the Dispatcher...
        /// </summary>
        /// <param name="a_aszCmd">arguments</param>
        /// <returns>true if the program should exit</returns>
        public delegate bool Function(string[] a_aszCmd);

        /// <summary>
        /// Map commands to functions...
        /// </summary>
        public class DispatchTable
        {
            /// <summary>
            /// Stock our entries...
            /// </summary>
            /// <param name="a_function">the function</param>
            /// <param name="a_aszCmd">command variants for this function</param>
            public DispatchTable(Function a_function, string[] a_aszCmd)
            {
                m_aszCmd = a_aszCmd;
                m_function = a_function;
            }

            /// <summary>
            /// Variations for this command...
            /// </summary>
            public string[] m_aszCmd;

            /// <summary>
            /// Function to call...
            /// </summary>
            public Function m_function;
        }

        #endregion


        // Private Attributes
        #region Private Attributes

        /// <summary>
        /// Our prompt...
        /// </summary>
        private string m_szPrompt;

        #endregion
    }
}
