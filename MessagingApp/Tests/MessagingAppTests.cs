using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MessagingApp.DTO;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace MessagingApp.Tests;


public class AuthControllerTests
{
    private readonly HttpClient _client;
    public AuthControllerTests()
    {
        var factory = new WebApplicationFactory<Program>()
             .WithWebHostBuilder(builder =>
             {
                 builder.ConfigureAppConfiguration((context, conf) =>
                 {
                     var settings = new Dictionary<string, string?>
                     {
                         // NOTE: keys use colon-delimited paths for sections
                         ["SecConfig:PrivateKey"] = "ThisIssUPErLongKWEYIFyUmuSTknOW7!@#SoINeEDtOPUshITTO64CharSYEAH?",
                         ["SecConfig:Issuer"] = "MessagingApp",
                         ["SecConfig:Audience"] = "MessagingAudience",
                         ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=demo_db;Username=postgres;Password=mysecretpw",
                         ["SecConfig:DurationDays"] = "7"
                     };
                     conf.AddInMemoryCollection(settings!);
                 });
             });

        _client = factory.CreateClient();
        _client.BaseAddress = new System.Uri("http://localhost:6865");
    }


    public async Task<CredentialsDTO> RegisterAndLoginUser()
    {
        var user = new
        {
            Username = Guid.NewGuid().ToString(),
            Password = "passwordmyass"
        };

        var response = await _client.PostAsJsonAsync("api/auth/register", user);
        response.EnsureSuccessStatusCode();
        Assert.Equal("User succesfully created", await response.Content.ReadAsStringAsync());

        response = await _client.PostAsJsonAsync("api/auth/login", user);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CredentialsDTO>();
        result.username = user.Username;
        result.password = user.Password;
        return result;
    }

    [Fact]
    public async Task RegisterAndLogin_registerAUserAndLogin_ReturnTokens()
    {
        var result = await RegisterAndLoginUser();
        Assert.NotNull(result.accessToken);
        Assert.NotEmpty(result.accessToken);
        Assert.NotNull(result.refreshToken);
        Assert.NotEmpty(result.refreshToken);
    }


    [Fact]
    public async Task SendMessage_InvalidRecepient_ReturnError()
    {
        var result = await RegisterAndLoginUser();

        MessageSendForm msg = new MessageSendForm()
        {
            DestinationUsername = Guid.NewGuid().ToString(),
            MessageText = "some message"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "api/message/send")
        {
            Content = JsonContent.Create(msg)
        };
        request.Headers.Add("Authorization", $"Bearer {result!.accessToken}");
        var response = await _client.SendAsync(request);
        Assert.Equal("NotFound", response.StatusCode.ToString());
    }

    [Fact]
    public async Task ReadMessage_SendMessageAndReadOnOtherSide_ReturnSentMessage()
    {
        var user1 = await RegisterAndLoginUser();
        var user2 = await RegisterAndLoginUser();

        MessageSendForm msgFromUser1 = new MessageSendForm()
        {
            DestinationUsername = user2.username!,
            MessageText = "some message"
        };

        var requestSend = new HttpRequestMessage(HttpMethod.Post, "api/message/send")
        {
            Content = JsonContent.Create(msgFromUser1)
        };
        Console.WriteLine(user1.accessToken);
        requestSend.Headers.Add("Authorization", $"Bearer {user1.accessToken}");
        var response = await _client.SendAsync(requestSend);
        Assert.Equal("OK", response.StatusCode.ToString());

        var requestRead = new HttpRequestMessage(HttpMethod.Get, "api/message/readReceived");
        requestRead.Headers.Add("Authorization", $"Bearer {user2.accessToken}");
        response = await _client.SendAsync(requestRead);

        var result = await response.Content.ReadFromJsonAsync<MessageReceivedDTO[]>();

        bool isUser1MessagePresent = false;
        bool messageCoincides = false;
        foreach (MessageReceivedDTO i in result)
        {
            if (i.SenderUsername == user1.username)
            {
                isUser1MessagePresent = true;
                if (i.MessageText == msgFromUser1.MessageText)
                {
                    messageCoincides = true;
                }
            }
        }
        Assert.True(isUser1MessagePresent);
        Assert.True(messageCoincides);
    }

    [Fact]
    public async Task ReadMessage_SendMessageReadTwoTimes_ReturnEmptyList()
    {
        var user1 = await RegisterAndLoginUser();
        var user2 = await RegisterAndLoginUser();

        MessageSendForm msgFromUser1 = new MessageSendForm()
        {
            DestinationUsername = user2.username!,
            MessageText = "some message"
        };

        var requestSend = new HttpRequestMessage(HttpMethod.Post, "api/message/send")
        {
            Content = JsonContent.Create(msgFromUser1)
        };
        Console.WriteLine(user1.accessToken);
        requestSend.Headers.Add("Authorization", $"Bearer {user1.accessToken}");
        var response = await _client.SendAsync(requestSend);
        Assert.Equal("OK", response.StatusCode.ToString());
        var request = new HttpRequestMessage(HttpMethod.Get, "api/message/readReceived");
        request.Headers.Add("Authorization", $"Bearer {user2.accessToken}");
        await _client.SendAsync(request);
        request = new HttpRequestMessage(HttpMethod.Get, "api/message/readReceived?unreadNotRead=true");
        request.Headers.Add("Authorization", $"Bearer {user2.accessToken}");
        response = await _client.SendAsync(request);


        var result = await response.Content.ReadFromJsonAsync<MessageReceivedDTO[]>();

        bool isUser1MessagePresent = false;
        foreach (MessageReceivedDTO i in result)
        {
            if (i.SenderUsername == user1.username)
            {
                isUser1MessagePresent = true;
            }
        }
        Assert.False(isUser1MessagePresent);
    }



    [Fact]
    public async Task Logout_SignInUserThenLogoutCheckAccess_ReturnUnauthorized()
    {
        var user1 = await RegisterAndLoginUser();

        var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/logout");
        request.Headers.Add("Authorization", $"Bearer {user1.accessToken}");
        await _client.SendAsync(request);
        request = new HttpRequestMessage(HttpMethod.Get, "api/auth");
        var result = await _client.SendAsync(request);
        Assert.Equal("Unauthorized", result.StatusCode.ToString());

        request = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh");
        request.Content = JsonContent.Create(new { refreshToken = user1.refreshToken });
        result = await _client.SendAsync(request);

        Assert.Equal("Unauthorized", result.StatusCode.ToString());
    }

    [Fact]
    public async Task ExpiredToken_GetExpiredTokenAndAccessResourse_ReturnUnauthorized()
    {
        var user1 = await RegisterAndLoginUser();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/RetrieveExpiredToken");
        request.Content = JsonContent.Create(new { Username = user1.username, Password = user1.password });
        var result = await _client.SendAsync(request);

        var creds = await result.Content.ReadFromJsonAsync<CredentialsDTO>();

        request = new HttpRequestMessage(HttpMethod.Get, "/api/auth");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"{creds!.accessToken}");
        result = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task CreateChat_RegisterUserAndCreateEmptyChat_ReturnSuccess()
    {
        var user1 = await RegisterAndLoginUser();
        var newChat = new { name = Guid.NewGuid().ToString(), members = new List<string>() };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chats/newChat");
        request.Content = JsonContent.Create(newChat);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.accessToken);
        var result = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        request = new HttpRequestMessage(HttpMethod.Get, "/api/chats/myChats");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.accessToken);
        result = await _client.SendAsync(request);
        bool chatPresent = false;
        foreach (var i in await result.Content.ReadFromJsonAsync<List<UserChatsDTO>>())
        {
            if (i.Name == newChat.name) chatPresent = true;
        }
        Assert.True(chatPresent);
    }

    [Fact]
    public async Task CreateChat_RegisterUserAndCreateEmptyChatWithSameName_ReturnBadRequest()
    {
        var user1 = await RegisterAndLoginUser();
        var chatname = Guid.NewGuid().ToString();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chats/newChat");
        request.Content = JsonContent.Create(new { name = chatname, members = new List<string>() });
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.accessToken);
        await _client.SendAsync(request);
        request = new HttpRequestMessage(HttpMethod.Post, "/api/chats/newChat");
        request.Content = JsonContent.Create(new { name = chatname, members = new List<string>() });
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.accessToken);
        var result = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task AddUser_RegisterUserAndCreateEmptyChatAddUser_ReturnOk()
    {
        var user1 = await RegisterAndLoginUser();
        var user2 = await RegisterAndLoginUser();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chats/newChat");
        request.Content = JsonContent.Create(new { name = Guid.NewGuid().ToString(), members = new List<string>() });
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.accessToken);
        await _client.SendAsync(request);
        request = new HttpRequestMessage(HttpMethod.Get, "/api/chats/myChats");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.accessToken);
        var result = await (await _client.SendAsync(request)).Content.ReadFromJsonAsync<List<UserChatsDTO>>();
        List<long> chatIDs = new List<long>();
        foreach (UserChatsDTO i in result)
        {
            chatIDs.Add((long)i.ID!);
            request = new HttpRequestMessage(HttpMethod.Post, "/api/chats/newMember");
            request.Content = JsonContent.Create(new { chatID = i.ID, members = new List<string>() { user2.username! } });
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.accessToken);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        request = new HttpRequestMessage(HttpMethod.Get, $"/api/chats/memberList?chatID={chatIDs[0]}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user1.accessToken);
        var result3 = await _client.SendAsync(request);
        bool userPresent = false;
        foreach (var i in await result3.Content.ReadFromJsonAsync<List<string>>())
        {
            if (i == user2.username) userPresent = true;
        }
        Assert.True(userPresent);
    }

}