using Microsoft.AspNetCore.Mvc;
using EasyCredit.API.Services; // Import Service AI
using Microsoft.AspNetCore.Authorization;
using EasyCredit.API.Data;     // Import Database
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EasyCredit.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly LoanRecommendationService _aiService;
    private readonly ApplicationDbContext _context; // <-- Đã thêm lại cái này

    // Inject cả 2 dịch vụ vào đây
    public ChatbotController(LoanRecommendationService aiService, ApplicationDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }

    // 1. API nhận input từ Chatbot -> Trả về gói vay (AI)
    [HttpPost("recommend-ai")]
    public IActionResult Recommend([FromBody] LoanInputDto input)
    {
        var predictedPackage = _aiService.Predict(input.Amount, input.Income, input.Term);
        object packageDetail = null;

        if (predictedPackage == "VIP")
        {
            packageDetail = new {
                Name = "👑 GÓI TÍN DỤNG VIP (AI Đề xuất)",
                Rate = "0.8%/tháng",
                Limit = "Đến 500 triệu",
                Desc = "Dựa trên thu nhập cao của bạn, đây là gói lãi suất thấp nhất."
            };
        }
        else if (predictedPackage == "STANDARD")
        {
            packageDetail = new {
                Name = "⭐ GÓI TIÊU DÙNG CHUẨN (AI Đề xuất)",
                Rate = "1.5%/tháng",
                Limit = "Đến 100 triệu",
                Desc = "Phù hợp với nhu cầu và thu nhập hiện tại của bạn."
            };
        }
        else 
        {
            packageDetail = new {
                Name = "🚀 GÓI KHỞI ĐỘNG (AI Đề xuất)",
                Rate = "0% tháng đầu",
                Limit = "Tối đa 15 triệu",
                Desc = "Gói hỗ trợ nhanh, thủ tục đơn giản cho khoản vay nhỏ."
            };
        }

        return Ok(new { 
            Prediction = predictedPackage, 
            Data = packageDetail,
            Message = "AI đã phân tích nhu cầu của bạn và tìm thấy gói phù hợp nhất:" 
        });
    }

    // 2. API Tra cứu trạng thái hồ sơ (Mới thêm)
    [HttpGet("check-status")]
    [Authorize]
    public async Task<IActionResult> CheckMyStatus()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        var userId = int.Parse(userIdString);

        // Dùng _context để tìm đơn vay
        var lastLoan = await _context.LoanApplications
                                     .Where(l => l.UserId == userId)
                                     .OrderByDescending(l => l.CreatedAt)
                                     .FirstOrDefaultAsync();

        if (lastLoan == null)
        {
            return Ok(new { Found = false, Message = "Bạn chưa có hồ sơ vay nào trên hệ thống." });
        }

        return Ok(new { 
            Found = true, 
            Id = lastLoan.Id, 
            Amount = lastLoan.Amount, 
            Status = lastLoan.Status,
            Date = lastLoan.CreatedAt.ToString("dd/MM/yyyy")
        });
    }
}

// Class DTO
public class LoanInputDto
{
    public float Amount { get; set; }
    public float Income { get; set; }
    public float Term { get; set; }
}