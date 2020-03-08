using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Utility
{
    using Miki.Functional;

    public static class Functional
    {

        public static Result<T> AsResult<T>(Func<T> func)
        {
            try
            {
                return new Result<T>(func());
            }
            catch(Exception ex)
            {
                return new Result<T>(ex);
            }
        }
    }
}
