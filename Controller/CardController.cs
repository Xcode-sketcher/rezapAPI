using Microsoft.AspNetCore.Mvc;
using rezapAPI.Model;

namespace rezapAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CardsController : ControllerBase
    {
        public List<Card> card = new List<Card>
        {
            new Card
            {
                Id = 1,
                Title = "TOTAL DE VENDAS",
                Value = "R$ 45.850,00",
                Icon = "💰"
            },
            new Card
            {
                Id = 2,
                Title = "CLIENTES",
                Value = "321",
                Icon = "👤"
            },
            new Card
            {
                Id = 3,
                Title = "PROJETOS ATIVOS",
                Value = "6",
                Icon = "📊"
            }
        };

        [HttpGet]
        public IActionResult GetAllCards()
        {
            return Ok(card);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetCardById(int id)
        {
            var foundCard = card.FirstOrDefault(n => n.Id == id);
            if (foundCard == null) return NotFound();
            return Ok(foundCard);
        }

        [HttpPost]
        public IActionResult CreateCard([FromBody] Card newCard)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            newCard.Id = card.Any() ? card.Max(n => n.Id) + 1 : 1;
            card.Add(newCard);
            return CreatedAtAction(nameof(GetCardById), new { id = newCard.Id }, newCard);
        }

        [HttpPut("{id:int}")]
        public IActionResult UpdateCard(int id, [FromBody] Card updatedCard)
        {
            var existingCard = card.FirstOrDefault(n => n.Id == id);
            if (existingCard == null) return NotFound();

            existingCard.Title = updatedCard.Title ?? existingCard.Title;
            existingCard.Value = updatedCard.Value ?? existingCard.Value;
            existingCard.Icon = updatedCard.Icon ?? existingCard.Icon;
            existingCard.Color = updatedCard.Color ?? existingCard.Color;

            return Ok(existingCard);
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteCard(int id)
        {
            var cardToDelete = card.FirstOrDefault(n => n.Id == id);
            if (cardToDelete == null) return NotFound();
            
            card.Remove(cardToDelete);
            return NoContent();
        }

    }

}