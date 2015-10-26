using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionNote.Common.Models
{
    public interface INotesRepository : IRepository<NoteItem, string>
    {
    }
}
