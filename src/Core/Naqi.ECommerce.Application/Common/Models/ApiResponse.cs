// src/Core/Naqi.ECommerce.Application/Common/Models/ApiResponse.cs
//
// Consistent JSON envelope for any controller action that returns JSON
// (AJAX endpoints in the Dashboard, or the Api project's REST endpoints).
// Using this everywhere instead of ad-hoc anonymous objects means the
// frontend/JS only ever needs to handle ONE response shape:
//
//   { "success": true,  "message": null,        "data": { ... } }
//   { "success": false, "message": "some error", "data": null }
//
// Use the non-generic ApiResponse for actions with no payload (e.g. a
// Delete action that just confirms success), and ApiResponse<T> when
// there's data to return alongside the success/message envelope.

namespace Naqi.ECommerce.Application.Common.Models;

public class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    public static ApiResponse Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(string message) =>
        new() { Success = false, Message = message };
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static new ApiResponse<T> Fail(string message) =>
        new() { Success = false, Data = default, Message = message };
}