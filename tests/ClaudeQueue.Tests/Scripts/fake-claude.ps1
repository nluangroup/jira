$mode = $env:FAKE_CLAUDE_MODE
$init = '{"type":"system","subtype":"init"}'
$agent = '{"type":"assistant","message":{"content":[{"type":"tool_use","name":"Agent","input":{"description":"Implement Task 1: Health endpoint"}}]}}'
$text = '{"type":"assistant","message":{"content":[{"type":"text","text":"Working on it"}]}}'
switch ($mode) {
    'success' {
        $init; $agent; $text
        '{"type":"result","is_error":false,"total_cost_usd":0.42,"result":"All done"}'
        exit 0
    }
    'error' {
        $init; $text
        '{"type":"result","is_error":true,"total_cost_usd":0.1,"result":"Budget exceeded"}'
        exit 0
    }
    'hang' {
        $init
        Start-Sleep 60
    }
}
