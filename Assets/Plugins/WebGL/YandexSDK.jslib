mergeInto(LibraryManager.library, {
  InitYandexSDK_JS: function () {
    if (window.ysdk) {
      console.log("[YSDK.jslib] SDK already initialized");
      return;
    }
    if (typeof YaGames !== 'undefined') {
      YaGames.init().then(function(ysdk) {
        window.ysdk = ysdk;
        console.log("[YSDK.jslib] SDK initialized");
      }).catch(function(e) {
        console.warn("[YSDK.jslib] Init error:", e);
      });
    } else {
      console.log("[YSDK.jslib] YaGames not found");
    }
  },

  GameReady_JS: function () {
    if (window.ysdk && window.ysdk.features && window.ysdk.features.LoadingAPI) {
      window.ysdk.features.LoadingAPI.ready();
      console.log("[YSDK.jslib] LoadingAPI.ready() called");
    } else {
      console.log("[YSDK.jslib] LoadingAPI not available");
    }
  },

  GameplayStart_JS: function () {
    if (window.ysdk && window.ysdk.features && window.ysdk.features.GameplayAPI) {
      window.ysdk.features.GameplayAPI.start();
      console.log("[YSDK.jslib] GameplayAPI.start() called");
    }
  },

  GameplayStop_JS: function () {
    if (window.ysdk && window.ysdk.features && window.ysdk.features.GameplayAPI) {
      window.ysdk.features.GameplayAPI.stop();
      console.log("[YSDK.jslib] GameplayAPI.stop() called");
    }
  },

  SaveUserData_JS: function (jsonStrPtr) {
    var jsonStr = UTF8ToString(jsonStrPtr);
    if (!window.ysdk) {
      console.log("[YSDK.jslib] SaveUserData (no ysdk):", jsonStr);
      return;
    }
    try {
      var dataObj = JSON.parse(jsonStr);
      window.ysdk.getPlayer().then(function(player) {
        player.setData(dataObj, true).then(function() {
          console.log("[YSDK.jslib] Player data saved successfully");
        }).catch(function(err) {
          console.warn("[YSDK.jslib] Save player data error:", err);
        });
      }).catch(function(err) {
        console.warn("[YSDK.jslib] Get player error:", err);
      });
    } catch(e) {
      console.error("[YSDK.jslib] JSON parse error in SaveUserData:", e);
    }
  },

  LoadUserData_JS: function (gameObjectNamePtr, callbackMethodNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var callbackMethodName = UTF8ToString(callbackMethodNamePtr);
    if (!window.ysdk) {
      console.log("[YSDK.jslib] LoadUserData (no ysdk)");
      SendMessage(gameObjectName, callbackMethodName, "{}");
      return;
    }
    window.ysdk.getPlayer().then(function(player) {
      player.getData().then(function(data) {
        var jsonStr = JSON.stringify(data || {});
        SendMessage(gameObjectName, callbackMethodName, jsonStr);
      }).catch(function(err) {
        console.warn("[YSDK.jslib] Load player data error:", err);
        SendMessage(gameObjectName, callbackMethodName, "{}");
      });
    }).catch(function(err) {
      console.warn("[YSDK.jslib] Get player error:", err);
      SendMessage(gameObjectName, callbackMethodName, "{}");
    });
  },

  SubmitLeaderboardScore_JS: function (leaderboardNamePtr, score) {
    var leaderboardName = UTF8ToString(leaderboardNamePtr);
    if (!window.ysdk) {
      console.log("[YSDK.jslib] SubmitLeaderboardScore (no ysdk):", leaderboardName, score);
      return;
    }
    window.ysdk.getLeaderboards().then(function(lb) {
      lb.setLeaderboardScore(leaderboardName, score).then(function() {
        console.log("[YSDK.jslib] Leaderboard score submitted:", leaderboardName, score);
      }).catch(function(err) {
        console.warn("[YSDK.jslib] Submit score error:", err);
      });
    }).catch(function(err) {
      console.warn("[YSDK.jslib] Get leaderboards error:", err);
    });
  },

  ShowFullscreenAdv_JS: function (gameObjectNamePtr, onOpenCallbackPtr, onCloseCallbackPtr, onErrorCallbackPtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var onOpen = UTF8ToString(onOpenCallbackPtr);
    var onClose = UTF8ToString(onCloseCallbackPtr);
    var onError = UTF8ToString(onErrorCallbackPtr);

    if (!window.ysdk || !window.ysdk.adv) {
      console.log("[YSDK.jslib] ShowFullscreenAdv (mock: immediate close)");
      SendMessage(gameObjectName, onOpen, "");
      setTimeout(function() {
        SendMessage(gameObjectName, onClose, "true");
      }, 100);
      return;
    }

    window.ysdk.adv.showFullscreenAdv({
      callbacks: {
        onOpen: function() {
          SendMessage(gameObjectName, onOpen, "");
        },
        onClose: function(wasShown) {
          SendMessage(gameObjectName, onClose, wasShown ? "true" : "false");
        },
        onError: function(error) {
          SendMessage(gameObjectName, onError, String(error));
        }
      }
    });
  },

  ShowRewardedVideo_JS: function (gameObjectNamePtr, onOpenCallbackPtr, onRewardedCallbackPtr, onCloseCallbackPtr, onErrorCallbackPtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var onOpen = UTF8ToString(onOpenCallbackPtr);
    var onRewarded = UTF8ToString(onRewardedCallbackPtr);
    var onClose = UTF8ToString(onCloseCallbackPtr);
    var onError = UTF8ToString(onErrorCallbackPtr);

    if (!window.ysdk || !window.ysdk.adv) {
      console.log("[YSDK.jslib] ShowRewardedVideo (mock: rewarded and closed)");
      SendMessage(gameObjectName, onOpen, "");
      SendMessage(gameObjectName, onRewarded, "");
      setTimeout(function() {
        SendMessage(gameObjectName, onClose, "");
      }, 100);
      return;
    }

    window.ysdk.adv.showRewardedVideo({
      callbacks: {
        onOpen: function() {
          SendMessage(gameObjectName, onOpen, "");
        },
        onRewarded: function() {
          SendMessage(gameObjectName, onRewarded, "");
        },
        onClose: function() {
          SendMessage(gameObjectName, onClose, "");
        },
        onError: function(error) {
          SendMessage(gameObjectName, onError, String(error));
        }
      }
    });
  }
});
