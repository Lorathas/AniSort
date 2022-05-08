// Copyright © 2020 Lorathas
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace AniDbSharp
{
    public enum CommandStatus
    {
        LoginAccepted = 200,
        LoginAcceptedNewVersionAvailable = 201,
        LoggedOut = 203,
        Resource = 205,
        Stats = 206,
        Top = 207,
        Uptime = 208,
        EncryptionEnabled = 209,
        MylistEntryAdded = 210,
        MylistEntryDeleted = 211,
        AddedFile = 214,
        AddedStream = 215,
        ExportQueued = 217,
        ExportCancelled = 218,
        EncodingChanged = 219,
        File = 220,
        Mylist = 221,
        MylistStats = 222,
        Wishlist = 223,
        Notification = 224,
        GroupStatus = 225,
        WishlistEntryAdded = 226,
        WishlistEntryDeleted = 227,
        WishlistEntryUpdated = 228,
        MultipleWishlist = 229,
        Anime = 230,
        AnimeBestMatch = 231,
        RandomAnime = 232,
        AnimeDescription = 233,
        Review = 234,
        Character = 235,
        Song = 236,
        AnimeTag= 237,
        CharacterTag= 238,
        Episode = 240,
        Updated = 243,
        Title = 244,
        Creator = 245,
        NotificationEntryAdded = 246,
        NotificationEntryDeleted = 247,
        NotificationEntryUpdated = 248,
        MultipleNotification = 249,
        Group = 250,
        Category = 251,
        BuddyList = 253,
        BuddyState = 254,
        BuddyAdded = 255,
        BuddyDeleted= 256,
        BuddyAccepted = 257,
        BuddyDenied = 258,
        Voted = 260,
        VoteFound = 261,
        VoteUpdated = 262,
        VoteRevoked = 263,
        HotAnime = 265,
        RandomRecommendation = 266,
        RandomSimilar = 267,
        NotificationEnabled = 270,
        NotifyAckSuccessfulMessage = 281,
        NotifyAckSuccessfulNotification = 282,
        NotificationState = 290,
        NotifyList = 291,
        NotifyGetMessage = 292,
        NotifyGetNotify = 293,
        SendMessageSuccessful = 294,
        UserId = 295,
        Calendar = 297,
        NoSuchFile = 320,
        MultipleFilesFound = 322,
        NoSuchEpisode = 340,
        LoginFailed = 500,
        LoginFirst = 501,
        AccessDenied = 502,
        ClientVersionOutdated = 503,
        ClientBanned = 504,
        IllegalInputOrAccessDenied = 505,
        InvalidSession = 506,
        Banned = 555,
        UnknownCommand = 598,
        InternalServerError = 600,
        AniDbOutOfService = 601,
        ServerBusy = 602,
        Timeout = 604
    }
}
