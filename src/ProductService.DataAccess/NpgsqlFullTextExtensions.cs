using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.DataAccess
{
    public static class NpgsqlFullTextExtensions
    {
        public static float TsRank(NpgsqlTsVector vector, NpgsqlTsQuery query)
          => throw new NotSupportedException("This method is for EF Core translation only");
    }
}
