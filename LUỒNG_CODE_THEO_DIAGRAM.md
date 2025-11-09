# 📚 LUỒNG CODE THEO SEQUENCE DIAGRAM - CHI TIẾT TỪNG BƯỚC

Tài liệu này mô tả chi tiết luồng code theo sequence diagram, với vị trí file và dòng code cụ thể cho từng bước.

---

## 🔵 PHẦN 1: ACCESS MY LOANS (Truy cập danh sách sách đã mượn)

### **Bước 1: `1: accessMyLoans()` - Member click vào MyLoansView**

**Mô tả:** Người dùng (Member) click vào link "My Loans" trên navigation bar.

**Vị trí code:**
- **File:** `Views/Shared/_Layout.cshtml`
- **Dòng:** **28**
- **Code:**
```html
<li class="nav-item"><a class="nav-link text-white" asp-controller="MyLoans" asp-action="Index">My Loans</a></li>
```

**Giải thích:**
- Link này sử dụng `asp-controller="MyLoans"` và `asp-action="Index"`
- Khi click, browser sẽ gửi GET request đến `/MyLoans/Index`
- Route này được map đến `BorrowingController.Index()` nhờ `[Route("MyLoans")]` attribute

---

### **Bước 2: `1.1: loadBorrowedBooks()` - MyLoansView gọi BorrowingController**

**Mô tả:** Request được xử lý bởi `BorrowingController.Index()` method.

**Vị trí code:**
- **File:** `Controller/BorrowingController.cs`
- **Dòng:** **33-72**
- **Code:**
```csharp
[HttpGet("")]
[HttpGet("Index")]
public async Task<IActionResult> Index(int? userId = null)
{
    // Get current user ID and role
    var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var roleClaim = User?.FindFirst(ClaimTypes.Role)?.Value;
    
    if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
    {
        // If not logged in, redirect to login
        return RedirectToAction("Login", "Account");
    }

    List<BorrowTransaction> borrowedList;

    // Check if user is Admin
    bool isAdmin = roleClaim == "Admin" || roleClaim == "Administrator";
    
    // If admin, show all borrow transactions (including pending requests from all members)
    if (isAdmin)
    {
        // Get all borrow transactions for admin view
        borrowedList = await _borrowingService.GetAllBorrowTransactions();
        ViewData["Title"] = "All Loans (Admin View)";
        ViewData["IsAdmin"] = true;
    }
    else
    {
        // For regular members, only show their own books
        // Use provided userId or current user's ID
        int targetUserId = userId ?? currentUserId;
        borrowedList = await _borrowingService.GetBorrowedBooks(targetUserId);
        ViewData["Title"] = "My Loans";
        ViewData["IsAdmin"] = false;
    }

    // Explicitly specify the view path since controller name is BorrowingController but view is in MyLoans folder
    return View("~/Views/MyLoans/Index.cshtml", borrowedList);
}
```

**Giải thích:**
- **Dòng 38-39:** Lấy User ID và Role từ Claims
- **Dòng 41-45:** Kiểm tra đăng nhập, nếu chưa đăng nhập thì redirect về Login
- **Dòng 50:** Kiểm tra xem user có phải Admin không
- **Dòng 53-58:** Nếu là Admin, gọi `GetAllBorrowTransactions()` để lấy tất cả transactions
- **Dòng 60-67:** Nếu là Member, gọi `GetBorrowedBooks(targetUserId)` để lấy chỉ sách của họ
- **Dòng 71:** Trả về View với model là `borrowedList`

---

### **Bước 3: `2: getBorrowedBooks(memberId)` - BorrowingController gọi BorrowingService**

**Mô tả:** Controller gọi Service để lấy dữ liệu từ database.

**Vị trí code:**
- **File:** `Services/BorrowingService.cs`
- **Dòng:** **23-34** (cho Member) hoặc **55-65** (cho Admin)

**Cho Member:**
```csharp
public async Task<List<BorrowTransaction>> GetBorrowedBooks(int memberId)
{
    var borrowedList = await _context.BorrowTransactions
        .Include(b => b.User)
        .Include(b => b.BorrowDetails)
            .ThenInclude(d => d.Book)
        .Where(b => b.UserId == memberId)
        .OrderByDescending(b => b.BorrowDate)
        .ToListAsync();

    return borrowedList;
}
```

**Cho Admin:**
```csharp
public async Task<List<BorrowTransaction>> GetAllBorrowTransactions()
{
    var allTransactions = await _context.BorrowTransactions
        .Include(b => b.User)
        .Include(b => b.BorrowDetails)
            .ThenInclude(d => d.Book)
        .OrderByDescending(b => b.BorrowDate)
        .ToListAsync();

    return allTransactions;
}
```

**Giải thích:**
- **Dòng 25-28:** Query database với Entity Framework, Include các related entities (User, BorrowDetails, Book)
- **Dòng 29:** Filter theo `UserId` (chỉ cho Member)
- **Dòng 30:** Sắp xếp theo ngày mượn giảm dần (mới nhất trước)
- **Dòng 31:** Execute query và trả về List
- **Dòng 33:** Return danh sách

---

### **Bước 4: `2.1: borrowedList` - BorrowingService trả về BorrowingController**

**Mô tả:** Service trả về danh sách `List<BorrowTransaction>` cho Controller.

**Vị trí code:**
- **File:** `Services/BorrowingService.cs`
- **Dòng:** **33** (cho Member) hoặc **64** (cho Admin)
- **Code:** `return borrowedList;` hoặc `return allTransactions;`

**Giải thích:**
- Service trả về `List<BorrowTransaction>` chứa tất cả thông tin cần thiết
- Mỗi `BorrowTransaction` đã được Include đầy đủ User, BorrowDetails, và Book

---

### **Bước 5: `1.2: borrowedList` - BorrowingController trả về MyLoansView**

**Mô tả:** Controller truyền dữ liệu vào View.

**Vị trí code:**
- **File:** `Controller/BorrowingController.cs`
- **Dòng:** **71**
- **Code:**
```csharp
return View("~/Views/MyLoans/Index.cshtml", borrowedList);
```

**Giải thích:**
- Controller chỉ định rõ đường dẫn view: `~/Views/MyLoans/Index.cshtml`
- Truyền `borrowedList` làm model cho View
- View sẽ nhận model này qua `@model IEnumerable<SWD.Models.BorrowTransaction>`

---

### **Bước 6: `1.3: displayBorrowedBooks(borrowedList)` - MyLoansView hiển thị UI**

**Mô tả:** View nhận dữ liệu và render HTML để hiển thị danh sách sách đã mượn.

**Vị trí code:**
- **File:** `Views/MyLoans/Index.cshtml`
- **Dòng:** **1-156** (toàn bộ phần hiển thị)

**Chi tiết từng phần:**

#### **6.1. Định nghĩa Model và Layout (Dòng 1-8)**
```razor
@{
    Layout = "_Layout";
}
@model IEnumerable<SWD.Models.BorrowTransaction>
@{
    ViewData["Title"] = ViewData["Title"] ?? "My Loans";
    var isAdmin = ViewData["IsAdmin"] as bool? ?? false;
}
```
- **Dòng 2:** Set layout là `_Layout.cshtml`
- **Dòng 4:** Định nghĩa model là `IEnumerable<BorrowTransaction>`
- **Dòng 6-7:** Lấy Title và IsAdmin từ ViewData

#### **6.2. Hiển thị Header (Dòng 10-14)**
```razor
<div class="card-header bg-gradient bg-primary text-white text-center rounded-top-4 py-3">
    <h2 class="fw-bold mb-0">📚 @(isAdmin ? "All Loans (Admin View)" : "My Loans")</h2>
</div>
```
- Hiển thị tiêu đề khác nhau cho Admin và Member

#### **6.3. Hiển thị Alert cho Admin (Dòng 23-29)**
```razor
@if (isAdmin && Model.Any())
{
    <div class="alert alert-warning alert-dismissible fade show mb-3" role="alert">
        <strong>Admin View:</strong> You are viewing all borrow transactions. Pending requests (Status: Processing) can be approved or rejected.
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
```

#### **6.4. Hiển thị Bảng Danh Sách (Dòng 37-147)**

**Table Header (Dòng 40-54):**
```razor
<table class="table table-hover table-bordered align-middle bg-white shadow-sm rounded-3">
    <thead class="table-primary text-center">
        <tr>
            <th>#</th>
            @if (isAdmin)
            {
                <th>Member</th>
            }
            <th>Book Title</th>
            <th>Borrow Date</th>
            <th>Due Date</th>
            <th>Status</th>
            <th>Fine (VND)</th>
            <th>Actions</th>
        </tr>
    </thead>
```

**Table Body - Loop qua từng BorrowTransaction (Dòng 56-143):**
```razor
<tbody>
    @{
        int index = 1;
    }
    @foreach (var borrow in Model)  // Dòng 60
    {
        foreach (var detail in borrow.BorrowDetails)  // Dòng 62
        {
            var isOverdue = borrow.DueDate < DateTime.Now && borrow.Status != "Returned";
            <tr class="@(isOverdue ? "table-danger bg-opacity-75" : "")" data-borrow-id="@borrow.BorrowId">
                <!-- Hiển thị thông tin sách -->
                <td class="text-center fw-semibold">@index</td>
                @if (isAdmin)
                {
                    <td class="fw-semibold">@(borrow.User?.FullName ?? "N/A")</td>
                }
                <td class="fw-semibold">@detail.Book.Title</td>
                <td>@borrow.BorrowDate.ToString("dd/MM/yyyy")</td>
                <td class="due-date">@borrow.DueDate.ToString("dd/MM/yyyy")</td>
                <!-- Status badges (Dòng 74-94) -->
                <!-- Actions buttons (Dòng 99-139) -->
            </tr>
            index++;
        }
    }
</tbody>
```

**Giải thích:**
- **Dòng 60:** Loop qua từng `BorrowTransaction` trong Model
- **Dòng 62:** Loop qua từng `BorrowDetail` (một transaction có thể có nhiều sách)
- **Dòng 64:** Kiểm tra xem có quá hạn không
- **Dòng 66-140:** Hiển thị thông tin từng dòng trong bảng

#### **6.5. Hiển thị Status Badges (Dòng 74-94)**
```razor
@if (borrow.Status == "Processing")
{
    <span class="badge bg-secondary text-light px-3 py-2">Processing</span>
}
else if (borrow.Status == "Borrowing" || borrow.Status == "Borrowed")
{
    <span class="badge bg-warning text-dark px-3 py-2">Borrowing</span>
}
else if (isOverdue)
{
    <span class="badge bg-danger bg-gradient px-3 py-2">Overdue</span>
}
else if (borrow.Status == "Returned")
{
    <span class="badge bg-success bg-gradient px-3 py-2">Returned</span>
}
```

#### **6.6. Hiển thị Action Buttons (Dòng 99-139)**
- **Dòng 100-111:** Nút Approve/Reject cho Admin (khi status = "Processing")
- **Dòng 113-134:** Nút Renew/Return cho owner (khi status = "Borrowing")

---

## 🟢 PHẦN 2: RENEWAL (Gia hạn sách) - OPTIONAL FLOW

### **Bước 1: `3: selectRenewal(borrowId)` - Member click nút Renew**

**Mô tả:** Người dùng click vào nút "Renew" trên một quyển sách.

**Vị trí code:**
- **File:** `Views/MyLoans/Index.cshtml`
- **Dòng:** **121-125** (HTML button)
- **Code:**
```html
<button type="button" 
        class="btn btn-sm btn-outline-primary me-2 shadow-sm js-renewal" 
        data-borrow-id="@borrow.BorrowId">
    <i class="bi bi-arrow-clockwise"></i> Renew
</button>
```

**Giải thích:**
- Button có class `js-renewal` để JavaScript có thể bắt sự kiện
- Attribute `data-borrow-id` chứa ID của BorrowTransaction

**JavaScript Event Listener:**
- **File:** `Views/MyLoans/Index.cshtml`
- **Dòng:** **204-212**
- **Code:**
```javascript
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.js-renewal').forEach(button => {
        button.addEventListener('click', async function () {
            const borrowId = this.getAttribute('data-borrow-id');
            
            if (!confirm('Are you sure you want to renew this book?')) {
                return;
            }
            // ... tiếp tục ở bước 2
```

**Giải thích:**
- **Dòng 205:** Đợi DOM load xong
- **Dòng 206:** Tìm tất cả button có class `js-renewal`
- **Dòng 207:** Thêm event listener cho mỗi button
- **Dòng 208:** Lấy `borrowId` từ attribute `data-borrow-id`
- **Dòng 210-212:** Hiển thị confirm dialog, nếu user cancel thì return

---

### **Bước 2: `1.4: requestRenewal(borrowId)` - MyLoansView gọi BorrowingController**

**Mô tả:** JavaScript gửi AJAX request đến Controller.

**Vị trí code:**
- **File:** `Views/MyLoans/Index.cshtml`
- **Dòng:** **214-226**
- **Code:**
```javascript
// Disable button during request
this.disabled = true;
const originalText = this.innerHTML;
this.innerHTML = '<i class="bi bi-hourglass-split"></i> Processing...';

try {
    // Call RequestRenewal API - corresponds to requestRenewal(borrowId) in sequence diagram
    const response = await fetch(`/MyLoans/RequestRenewal?borrowId=${borrowId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        }
    });
```

**Giải thích:**
- **Dòng 215-217:** Disable button và đổi text thành "Processing..." để user biết đang xử lý
- **Dòng 221:** Gửi POST request đến `/MyLoans/RequestRenewal?borrowId={borrowId}`
- Sử dụng `fetch` API để gửi AJAX request

---

### **Bước 3: `4: processRenewal(borrowId)` - BorrowingController gọi BorrowingService**

**Mô tả:** Controller nhận request và gọi Service để xử lý logic gia hạn.

**Vị trí code:**
- **File:** `Controller/BorrowingController.cs`
- **Dòng:** **78-116** (method `RequestRenewal`)

**Chi tiết:**

#### **3.1. Kiểm tra Authentication (Dòng 81-84)**
```csharp
if (User?.Identity?.IsAuthenticated != true)
{
    return Json(new { ok = false, msg = "Please login to renew books." });
}
```

#### **3.2. Lấy User ID (Dòng 86-91)**
```csharp
var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
{
    return Json(new { ok = false, msg = "User not identified." });
}
```

#### **3.3. Verify Ownership (Dòng 93-97)**
```csharp
var borrow = await _context.BorrowTransactions.FirstOrDefaultAsync(b => b.BorrowId == borrowId);
if (borrow == null || borrow.UserId != userId)
{
    return Json(new { ok = false, msg = "Borrow transaction not found or access denied." });
}
```

#### **3.4. Gọi Service (Dòng 99-101)**
```csharp
// Process renewal through service
// Corresponds to: processRenewal(borrowId) in sequence diagram
var renewalStatus = await _borrowingService.ProcessRenewal(borrowId);
```

#### **3.5. Trả về JSON Response (Dòng 103-115)**
```csharp
if (renewalStatus.Success)
{
    return Json(new
    {
        ok = true,
        msg = renewalStatus.Message,
        newDueDate = renewalStatus.NewDueDate?.ToString("dd/MM/yyyy")
    });
}
else
{
    return Json(new { ok = false, msg = renewalStatus.Message });
}
```

---

### **Bước 4: `4: processRenewal(borrowId)` - BorrowingService xử lý logic**

**Mô tả:** Service thực hiện logic kiểm tra điều kiện và gia hạn sách.

**Vị trí code:**
- **File:** `Services/BorrowingService.cs`
- **Dòng:** **71-110** (method `ProcessRenewal`)

**Chi tiết:**

#### **4.1. Lấy BorrowTransaction (Dòng 73-83)**
```csharp
var borrow = await _context.BorrowTransactions
    .FirstOrDefaultAsync(b => b.BorrowId == borrowId);

if (borrow == null)
{
    return new RenewalStatus
    {
        Success = false,
        Message = "Borrow transaction not found."
    };
}
```

#### **4.2. Kiểm tra điều kiện [isEligible] (Dòng 85-98)**
```csharp
// Check if eligible for renewal
if (IsEligible(borrow))
{
    // Update due date (extend by 7 days, but not beyond 6 months from borrow date)
    UpdateDueDate(borrow);
    await _context.SaveChangesAsync();

    return new RenewalStatus
    {
        Success = true,
        Message = "Book renewal successful. Due date extended by 7 days.",
        NewDueDate = borrow.DueDate
    };
}
```

**Method `IsEligible()` - Dòng 116-133:**
```csharp
private bool IsEligible(BorrowTransaction borrow)
{
    // Cannot renew if already returned
    if (borrow.ReturnDate != null)
        return false;

    // Cannot renew if status is not "Borrowing" or "Borrowed"
    if (borrow.Status != "Borrowing" && borrow.Status != "Borrowed")
        return false;

    // Cannot renew if maximum renewal period (6 months from borrow date) would be exceeded
    var maxDueDate = borrow.BorrowDate.AddMonths(6);
    if (borrow.DueDate.AddDays(7) > maxDueDate)
        return false;

    return true;
}
```

**Method `UpdateDueDate()` - Dòng 139-154:**
```csharp
private void UpdateDueDate(BorrowTransaction borrow)
{
    var maxDueDate = borrow.BorrowDate.AddMonths(6);
    var newDueDate = borrow.DueDate.AddDays(7);

    // Extend by 7 days, but not beyond 6 months from borrow date
    if (newDueDate <= maxDueDate)
    {
        borrow.DueDate = newDueDate;
    }
    else
    {
        // If extending would exceed max, set to max
        borrow.DueDate = maxDueDate;
    }
}
```

#### **4.3. Xử lý [isNotEligible] (Dòng 99-109)**
```csharp
else
{
    // Log rejection
    await LogRejection(borrow, "Book is not eligible for renewal.");
    
    return new RenewalStatus
    {
        Success = false,
        Message = "Book is not eligible for renewal. It may have been returned, is overdue, or maximum renewal period has been reached."
    };
}
```

**Method `LogRejection()` - Dòng 160-172:**
```csharp
private async Task LogRejection(BorrowTransaction borrow, string reason)
{
    var log = new Log
    {
        UserId = borrow.UserId,
        Action = "Renewal Rejected",
        Description = $"Renewal rejected for Borrow ID {borrow.BorrowId}. Reason: {reason}",
        TimeStamp = DateTime.Now
    };

    _context.Logs.Add(log);
    await _context.SaveChangesAsync();
}
```

---

### **Bước 5: `4.3: renewalStatus` - BorrowingService trả về BorrowingController**

**Mô tả:** Service trả về `RenewalStatus` object.

**Vị trí code:**
- **File:** `Services/BorrowingService.cs`
- **Dòng:** **92-97** (success) hoặc **104-108** (failure)

**RenewalStatus Class - Dòng 179-184:**
```csharp
public class RenewalStatus
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? NewDueDate { get; set; }
}
```

---

### **Bước 6: `1.5: renewalStatus` - BorrowingController trả về MyLoansView**

**Mô tả:** Controller trả về JSON response cho JavaScript.

**Vị trí code:**
- **File:** `Controller/BorrowingController.cs`
- **Dòng:** **103-115**
- **Code:** `return Json(...)` - đã được mô tả ở Bước 3.5

---

### **Bước 7: `3.2: showRenewalResult(renewalStatus)` - MyLoansView hiển thị kết quả**

**Mô tả:** JavaScript nhận JSON response và cập nhật UI.

**Vị trí code:**
- **File:** `Views/MyLoans/Index.cshtml`
- **Dòng:** **228-270**

**Chi tiết:**

#### **7.1. Parse JSON Response (Dòng 228-230)**
```javascript
const data = await response.json();
const alertEl = document.getElementById('renewalAlert');
const messageEl = document.getElementById('renewalMessage');
```

#### **7.2. Xử lý Success (Dòng 232-247)**
```javascript
if (data.ok) {
    // Success - show success message and update due date
    alertEl.className = 'alert alert-success alert-dismissible fade show';
    messageEl.textContent = data.msg + (data.newDueDate ? ` New due date: ${data.newDueDate}` : '');
    
    // Update due date in the table
    const row = this.closest('tr');
    const dueDateCell = row.querySelector('.due-date');
    if (dueDateCell && data.newDueDate) {
        dueDateCell.textContent = data.newDueDate;
    }

    // Reload page after 2 seconds to refresh status
    setTimeout(() => {
        window.location.reload();
    }, 2000);
}
```

**Giải thích:**
- **Dòng 234:** Set alert thành success (màu xanh)
- **Dòng 235:** Hiển thị message kèm newDueDate nếu có
- **Dòng 238-241:** Tìm row chứa button và cập nhật due date trong table
- **Dòng 245-247:** Reload page sau 2 giây để refresh status

#### **7.3. Xử lý Error (Dòng 248-256)**
```javascript
else {
    // Error - show error message
    alertEl.className = 'alert alert-danger alert-dismissible fade show';
    messageEl.textContent = data.msg || 'Renewal request failed.';
    
    // Re-enable button
    this.disabled = false;
    this.innerHTML = originalText;
}
```

**Giải thích:**
- **Dòng 250:** Set alert thành error (màu đỏ)
- **Dòng 251:** Hiển thị error message
- **Dòng 254-255:** Re-enable button và restore text gốc

#### **7.4. Hiển thị Alert (Dòng 258)**
```javascript
alertEl.classList.remove('d-none');
```

#### **7.5. Error Handling (Dòng 259-270)**
```javascript
catch (error) {
    console.error('Error:', error);
    const alertEl = document.getElementById('renewalAlert');
    const messageEl = document.getElementById('renewalMessage');
    alertEl.className = 'alert alert-danger alert-dismissible fade show';
    messageEl.textContent = 'An error occurred while processing your request.';
    alertEl.classList.remove('d-none');
    
    // Re-enable button
    this.disabled = false;
    this.innerHTML = originalText;
}
```

---

## 📊 TÓM TẮT MAPPING DIAGRAM → CODE

| Diagram Step | File | Dòng Code | Mô tả |
|--------------|------|-----------|-------|
| `1: accessMyLoans()` | `Views/Shared/_Layout.cshtml` | 28 | Link "My Loans" |
| `1.1: loadBorrowedBooks()` | `Controller/BorrowingController.cs` | 35-72 | Method `Index()` |
| `2: getBorrowedBooks(memberId)` | `Services/BorrowingService.cs` | 23-34 | Method `GetBorrowedBooks()` |
| `2.1: borrowedList` | `Services/BorrowingService.cs` | 33 | Return statement |
| `1.2: borrowedList` | `Controller/BorrowingController.cs` | 71 | Return View |
| `1.3: displayBorrowedBooks()` | `Views/MyLoans/Index.cshtml` | 1-156 | Toàn bộ View rendering |
| `3: selectRenewal(borrowId)` | `Views/MyLoans/Index.cshtml` | 121-125, 204-212 | Button + Event listener |
| `1.4: requestRenewal(borrowId)` | `Views/MyLoans/Index.cshtml` | 221 | Fetch API call |
| `4: processRenewal(borrowId)` | `Controller/BorrowingController.cs` | 78-116 | Method `RequestRenewal()` |
| `4: processRenewal(borrowId)` | `Services/BorrowingService.cs` | 71-110 | Method `ProcessRenewal()` |
| `[isEligible]` | `Services/BorrowingService.cs` | 116-133 | Method `IsEligible()` |
| `4.1: updateDueDate()` | `Services/BorrowingService.cs` | 139-154 | Method `UpdateDueDate()` |
| `[isNotEligible]` | `Services/BorrowingService.cs` | 99-109 | Else branch |
| `4.2: logRejection()` | `Services/BorrowingService.cs` | 160-172 | Method `LogRejection()` |
| `4.3: renewalStatus` | `Services/BorrowingService.cs` | 92-97, 104-108 | Return RenewalStatus |
| `1.5: renewalStatus` | `Controller/BorrowingController.cs` | 103-115 | Return Json |
| `3.2: showRenewalResult()` | `Views/MyLoans/Index.cshtml` | 228-270 | JavaScript update UI |

---

## 🔑 ĐIỂM QUAN TRỌNG

1. **Route Mapping:**
   - Controller tên `BorrowingController` nhưng route là `/MyLoans` nhờ `[Route("MyLoans")]`
   - View nằm ở `Views/MyLoans/Index.cshtml` nên phải chỉ định rõ path trong `return View()`

2. **Service Layer:**
   - Tất cả business logic nằm trong `BorrowingService`
   - Controller chỉ điều phối, không chứa logic nghiệp vụ

3. **AJAX Renewal:**
   - Renewal sử dụng AJAX, không reload trang
   - Response là JSON format
   - UI được cập nhật động bằng JavaScript

4. **Error Handling:**
   - Có try-catch trong JavaScript
   - Service trả về `RenewalStatus` với Success flag
   - Controller trả về JSON với `ok` flag

---

## ✅ KẾT LUẬN

Tài liệu này đã mô tả chi tiết từng bước trong sequence diagram với vị trí file và dòng code cụ thể. Mỗi bước đều có giải thích rõ ràng về chức năng và cách hoạt động.

