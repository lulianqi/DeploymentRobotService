
function myTest(num1, num2, dotnetHelper)
{
    dotnetHelper.invokeMethodAsync('TestCall2');
    myCalculator(num1, num2, function (res) {
        console.log('执行成功,返回值为', res)
    });

    myCalculator(num1, num2, function (res) {
        dotnetHelper.invokeMethodAsync('TestCall', res);
    });
    return "hi hi";
}

function myCalculator(num1, num2, myCallback) {
    let sum = num1 + num2;
    myCallback(sum);
}

function myFsConfig(appId, timestamp, nonceStr, signature) {
    window.h5sdk.config({
        appId: appId,         // 必填，应用ID
        timestamp: timestamp,      // 必填，生成签名的时间戳，毫秒级
        nonceStr: nonceStr,      // 必填，生成签名的随机串
        signature: signature,     // 必填，签名
        jsApiList: ['getBlockActionSourceDetail','getUserInfo'],
        onSuccess: function (result) {
            //alert(JSON.stringify(result));
            // 成功回调，可以在成功之后使用 tt.xx jsapi
        },
        onFail: function () {
            // 失败回调
            alert("myFsConfig fail");
        }
    });
    window.h5sdk.error(error => alert(JSON.stringify(error)));
}

function dealFeishuBuildMessage(dotnetHelper) {
    window.h5sdk.ready(() => {
        let launchQuery = new URLSearchParams(location.search).get("bdp_launch_query");
        if (!launchQuery) {
            alert("bdp_launch_query not found in URL");
            return;
        }
        launchQuery = JSON.parse(launchQuery);
        const triggerCode = launchQuery.__trigger_id__;

        //获取当前操作用户信息
        tt.getUserInfo({
            withCredentials: true,
            success(res) {
                //tt.setClipboardData({ data: JSON.stringify(res) });
                let nickName = res.userInfo.nickName;
                //获取消息内容
                tt.getBlockActionSourceDetail({
                    triggerCode: triggerCode,
                    success(res) {
                        //alert(JSON.parse(res.content.messages[0].content));
                        //tt.setClipboardData({ data: JSON.stringify(res) });
                        //get message content
                        let messageContent = (JSON.parse(res.content.messages[0].content.toString())).text;
                        let messageSender = res.content.messages[0].sender.open_id;
                        let messageChatId = res.content.messages[0].openChatId;
                        dotnetHelper.invokeMethodAsync('DealBuildMessage', { "sender": messageSender, "content": messageContent, "trigger": nickName, "chatId": messageChatId });
                    },
                    fail(res) {
                        dotnetHelper.invokeMethodAsync('ShowErrorInfo', `getBlockActionSourceDetail fail: ${JSON.stringify(res)}`);
                        //alert(JSON.stringify(res));
                    }
                });
            },
            fail(res) {
                dotnetHelper.invokeMethodAsync('ShowErrorInfo', `getUserInfo fail: ${JSON.stringify(res)}`);
                //console.log(`getUserInfo fail: ${JSON.stringify(res)}`);
            }
        });
      
    });
}

function sendMessageCard(mesObj) {
    tt.sendMessageCard({
        "shouldChooseChat": false,
        "chooseChatParams": {},
        "openChatIDs": [
            mesObj.chatId
        ],
        "triggerCode": "testCode",
        "cardContent": {
            "msg_type": "interactive",
            "update_multi": false,
            "card": {
                "elements": [
                    {
                        "tag": "div",
                        "text": {
                            "tag": "plain_text",
                            "content": mesObj.content
                        }
                    }
                ]
            }
        },
        "withAdditionalMessage": true,
        success(res) {
            console.log(JSON.stringify(res));
            mesObj.dotnetHelper.invokeMethodAsync('ReportJsFuncStatus', 11 ,`ReportJsFuncStatus : ${JSON.stringify(res)}`);

        },
        fail(res) {
            console.log(`sendMessageCard fail: ${JSON.stringify(res)}`);
            //mesObj.dotnetHelper.invokeMethodAsync('ShowErrorInfo', `sendMessageCard fail: ${JSON.stringify(res)}`);
        }
    });
}

function myFsGetChartMessage(dotnetHelper) {
    window.h5sdk.ready(() => {
        let launchQuery = new URLSearchParams(location.search).get("bdp_launch_query");
        if (!launchQuery) {
            console.log("bdp_launch_query not found in URL");
            return;
        }
        launchQuery = JSON.parse(launchQuery);
        const triggerCode = launchQuery.__trigger_id__;

        tt.getBlockActionSourceDetail({
            triggerCode: triggerCode,
            success(res) {
                //alert(JSON.parse(res.content.messages[0].content));
                tt.setClipboardData({ data: JSON.stringify(res) });
                //get message content
                let messageContent = (JSON.parse(res.content.messages[0].content.toString())).text;
                let messageSender = res.content.messages[0].sender.open_id;
                dotnetHelper.invokeMethodAsync('DealBuildMessage', { "sender": messageSender, "content": messageContent });

                //myCallback({ "sender": "Taobao", "content": "www.taobao.com" });
            },
            fail(res) {
                alert(JSON.stringify(res));
            }
        });

        tt.getUserInfo({
            withCredentials: true,
            success(res) {
                tt.setClipboardData({ data: JSON.stringify(res) });
            },
            fail(res) {
                console.log(`getUserInfo fail: ${JSON.stringify(res)}`);
            }
        });

    });
}

function mySendMessageCard() {
    tt.sendMessageCard({
        "shouldChooseChat": false,
        "chooseChatParams": {},
        "openChatIDs": [
            "oc_d3f26c7ffdd91f8962a87b79878fa028"
        ],
        "triggerCode": "testCode",
        "cardContent": {
            "msg_type": "interactive",
            "update_multi": false,
            "card": {
                "elements": [
                    {
                        "tag": "div",
                        "text": {
                            "tag": "plain_text",
                            "content": "Content module"
                        }
                    }
                ]
            }
        },
        "withAdditionalMessage": true,
        success(res) {
            console.log(JSON.stringify(res));
        },
        fail(res) {
            console.log(`sendMessageCard fail: ${JSON.stringify(res)}`);
        }
    });
}

function copyText(text) {
    if (typeof (navigator.clipboard) == 'undefined') {
        console.log('navigator.clipboard');
        var textArea = document.createElement("textarea");
        textArea.value = text;
        textArea.style.position = "fixed";
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();

        try {
            var successful = document.execCommand('copy');
            var msg = successful ? 'successful' : 'unsuccessful';
            console.log(msg);
        } catch (err) {
            console.log('Was not possible to copy te text: ', err);
        }

        document.body.removeChild(textArea)
        return;
    }
    navigator.clipboard.writeText(text).then(function () {
        console.log(`successful!`);
    }, function (err) {
        console.log('unsuccessful!', err);
    });
}

function getIpInfo(dotnetHelper) {
    var url = "https://sipv4.com/json/";
    var xhr = new XMLHttpRequest();
    xhr.open("GET", url, true);
    xhr.send();
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4 && xhr.status == 200) {
            var json = xhr.responseText;
            console.log(json);
            //DotNet.invokeMethodAsync('SipServerApp', 'UpdateMessageCaller', json);
            dotnetHelper.invokeMethodAsync('SipServerApp', json);
            dotnetHelper.dispose();
            return json;
        }
    };
}
