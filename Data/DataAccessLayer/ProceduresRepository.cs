using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
namespace DataAccessLayer
{
    public class ProceduresRepository : BaseRepository<MedicalProcedure, int>
    {

        public ProceduresRepository(string connectionString) : base(connectionString) { }

        public override Task AddAsync(MedicalProcedure entity)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public override Task<List<MedicalProcedure>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public override Task<MedicalProcedure> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> UpdateAsync(MedicalProcedure entity)
        {
            throw new NotImplementedException();
        }
    }
}
