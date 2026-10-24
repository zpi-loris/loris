using Microsoft.EntityFrameworkCore;

namespace app.Messages
{
    public interface IDbWriteMessage
    {
        public virtual async void SaveToDb(DbContext context) { }
    }
}
