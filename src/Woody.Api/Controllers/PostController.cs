using Microsoft.AspNetCore.Mvc;
using Woody.Application.DTOs.Post;
using Woody.Application.UseCases.Posts.CreatePost;
using Woody.Application.UseCases.Posts.GetPost;

namespace Woody.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly CreatePostHandler _handler;
        private readonly GetPostHandler _getPostHandler;

        public PostController(CreatePostHandler handler, GetPostHandler getPostHandler)
        {
            _handler = handler;
            _getPostHandler = getPostHandler;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(CreatePostRequestDTO request)
        {
            var result = await _handler.Handle(request);
            return CreatedAtAction(nameof(CreatePost), new { id = result }, null);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var result = await _getPostHandler.HandleGetById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("topic/{topicId}")]
        public async Task<IActionResult> GetPostByTopic(int topicId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _getPostHandler.HandleGetByTopicId(topicId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetPostByUser(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _getPostHandler.HandleGetByUserId(userId, page, pageSize);
            return Ok(result);
        }
    }
}