using Microsoft.AspNetCore.Mvc;
using rezapAPI.Model;

namespace rezapAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ColumnsController : ControllerBase
    {
        // Dados em memória (você pode substituir por banco de dados)
        private static List<Column> _columns = new List<Column>
        {
            new Column { Id = "todo", Title = "A Fazer", Order = 1, Color = "#3b82f6" },
            new Column { Id = "in-progress", Title = "Em Progresso", Order = 2, Color = "#f59e0b" },
            new Column { Id = "done", Title = "Concluído", Order = 3, Color = "#10b981" }
        };

        // GET: api/columns
        [HttpGet]
        public ActionResult<IEnumerable<Column>> GetAll()
        {
            return Ok(_columns.OrderBy(c => c.Order));
        }

        // GET: api/columns/{id}
        [HttpGet("{id}")]
        public ActionResult<Column> GetById(string id)
        {
            var column = _columns.FirstOrDefault(c => c.Id == id);
            if (column == null)
                return NotFound();
            
            return Ok(column);
        }

        // POST: api/columns
        [HttpPost]
        public ActionResult<Column> Create([FromBody] Column column)
        {
            if (string.IsNullOrWhiteSpace(column.Title))
                return BadRequest("Title is required");

            // Gerar ID único
            column.Id = $"col-{DateTime.UtcNow.Ticks}";
            column.Order = _columns.Count + 1;
            
            // Gerar cor aleatória se não fornecida
            if (string.IsNullOrWhiteSpace(column.Color))
            {
                var random = new Random();
                column.Color = $"#{random.Next(0x1000000):X6}";
            }

            _columns.Add(column);
            return CreatedAtAction(nameof(GetById), new { id = column.Id }, column);
        }

        // PUT: api/columns/{id}
        [HttpPut("{id}")]
        public ActionResult<Column> Update(string id, [FromBody] Column updatedColumn)
        {
            var column = _columns.FirstOrDefault(c => c.Id == id);
            if (column == null)
                return NotFound();

            column.Title = updatedColumn.Title ?? column.Title;
            column.Order = updatedColumn.Order;
            column.Color = updatedColumn.Color ?? column.Color;

            return Ok(column);
        }

        // DELETE: api/columns/{id}
        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            var column = _columns.FirstOrDefault(c => c.Id == id);
            if (column == null)
                return NotFound();

            if (_columns.Count <= 1)
                return BadRequest("Cannot delete the last column");

            _columns.Remove(column);
            return NoContent();
        }

        // PUT: api/columns/reorder
        [HttpPut("reorder")]
        public ActionResult ReorderColumns([FromBody] List<string> columnIds)
        {
            for (int i = 0; i < columnIds.Count; i++)
            {
                var column = _columns.FirstOrDefault(c => c.Id == columnIds[i]);
                if (column != null)
                {
                    column.Order = i + 1;
                }
            }

            return Ok(_columns.OrderBy(c => c.Order));
        }
    }
}
