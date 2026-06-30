package main

import (
	"encoding/json"
	"fmt"
	"net/http"
	"sync"
	"time"

	"github.com/gorilla/websocket"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

// ---------- MODELS ----------

type Player struct {
	conn      *websocket.Conn
	score     int
	name      string
	sessionID string
}

type Question struct {
	Q       string   `json:"q"`
	Options []string `json:"options"`
	Answer  string   `json:"-"`
}

// ---------- GAME DATA ----------

var questions = []Question{
	{"Which country has won the most FIFA World Cups?", []string{"Germany", "Italy", "Brazil", "Argentina"}, "Brazil"},
	{"Who is the all-time top scorer in FIFA World Cup history?", []string{"Pelé", "Diego Maradona", "Miroslav Klose", "Gerd Müller"}, "Miroslav Klose"},
	{"Which team won the Premier League in 2023?", []string{"Manchester City", "Manchester United", "Liverpool", "Arsenal"}, "Manchester City"},
	{"Who won the Ballon d'Or in 2022?", []string{"Kylian Mbappé", "Karim Benzema", "Lionel Messi", "Cristiano Ronaldo"}, "Karim Benzema"},
	{"Which country hosted the 2018 FIFA World Cup?", []string{"Brazil", "Russia", "Qatar", "Canada"}, "Russia"},
	{"How many times has Cristiano Ronaldo won the UEFA Champions League?", []string{"4 times", "5 times", "6 times", "7 times"}, "5 times"},
	{"Which player has the most goals in Premier League history?", []string{"Harry Kane", "Alan Shearer", "Sergio Agüero", "Thierry Henry"}, "Alan Shearer"},
	{"In which year did Barcelona win their first UEFA Champions League?", []string{"1989", "1992", "1996", "2001"}, "1992"},
	{"Who is considered the greatest footballer of all time?", []string{"Pelé", "Maradona", "Messi", "Ronaldo"}, "Messi"},
	{"Which team won the 2022 FIFA World Cup?", []string{"France", "Argentina", "Brazil", "Germany"}, "Argentina"},
	{"How many times has Lionel Messi won the Ballon d'Or?", []string{"6 times", "7 times", "8 times", "9 times"}, "8 times"},
	{"Which English club has won the most UEFA Champions Leagues?", []string{"Liverpool", "Manchester United", "Chelsea", "Arsenal"}, "Liverpool"},
	{"Who was the manager of Manchester United during their treble win in 1999?", []string{"Roy Keane", "Sir Alex Ferguson", "David Moyes", "Louis van Gaal"}, "Sir Alex Ferguson"},
	{"Which player transferred from Barcelona to Manchester City in 2021?", []string{"Philippe Coutinho", "Lionel Messi", "Griezmann", "Dembélé"}, "Lionel Messi"},
	{"How many goals did Cristiano Ronaldo score in the 2007-08 season?", []string{"42", "50", "55", "60"}, "42"},
	{"Which country won the UEFA Euro 2020 (held in 2021)?", []string{"Italy", "England", "Denmark", "Spain"}, "Italy"},
	{"Who won the Ballon d'Or in 2021?", []string{"Kylian Mbappé", "Lionel Messi", "Robert Lewandowski", "Karim Benzema"}, "Lionel Messi"},
	{"Which team plays at the Etihad Stadium?", []string{"Liverpool", "Manchester City", "Arsenal", "Chelsea"}, "Manchester City"},
	{"How many Premier League titles has Manchester United won?", []string{"11", "12", "13", "20"}, "13"},
	{"Who was the first non-British/Irish manager to win the Premier League?", []string{"José Mourinho", "Arsène Wenger", "Pep Guardiola", "Carlo Ancelotti"}, "José Mourinho"},
	{"Which player has won the most UEFA Champions League titles?", []string{"Cristiano Ronaldo", "Lionel Messi", "Zinedine Zidane", "Andriy Shevchenko"}, "Cristiano Ronaldo"},
	{"In what year did Liverpool last win the Premier League before 2020?", []string{"1987", "1990", "1992", "2001"}, "1990"},
	{"Who is the all-time top scorer in English Premier League?", []string{"Wayne Rooney", "Alan Shearer", "Harry Kane", "Sergio Agüero"}, "Alan Shearer"},
	{"Which team won the FA Cup in 2022?", []string{"Arsenal", "Liverpool", "Manchester City", "Chelsea"}, "Manchester City"},
	{"How many times has Bayern Munich won the UEFA Champions League?", []string{"5 times", "6 times", "7 times", "8 times"}, "6 times"},
	{"Who scored the fastest goal in World Cup history?", []string{"Hakan Şuk", "Nawaf Al-Abed", "Clint Dempsey", "Vaclav Masek"}, "Nawaf Al-Abed"},
	{"Which player won the Golden Boot at the 2022 World Cup?", []string{"Kylian Mbappé", "Harry Kane", "Gavi", "Lionel Messi"}, "Kylian Mbappé"},
	{"In which year was the Premier League established?", []string{"1988", "1990", "1992", "1995"}, "1992"},
	{"How many goals did Pelé score in his career?", []string{"700", "759", "812", "1000"}, "759"},
	{"Which country has hosted the most FIFA World Cups?", []string{"Mexico", "Germany", "France", "England"}, "Mexico"},
	{"Who won the Ballon d'Or in 2020?", []string{"Lionel Messi", "Cristiano Ronaldo", "Robert Lewandowski", "Neymar"}, "Lionel Messi"},
	{"Which team won the UEFA Europa League in 2022?", []string{"Rangers", "Eintracht Frankfurt", "Roma", "Juventus"}, "Eintracht Frankfurt"},
	{"How many times has Zinedine Zidane won the UEFA Champions League as a manager?", []string{"2 times", "3 times", "4 times", "5 times"}, "3 times"},
	{"Which player has won the most Ballon d'Or awards?", []string{"Cristiano Ronaldo", "Pelé", "Lionel Messi", "George Weah"}, "Lionel Messi"},
	{"In what year did Arsenal last win the Premier League?", []string{"2002", "2004", "2006", "2008"}, "2004"},
	{"Who is the all-time top scorer for the England national team?", []string{"Raheem Sterling", "Harry Kane", "Wayne Rooney", "Gary Lineker"}, "Harry Kane"},
	{"Which team won the UEFA Champions League in 2023?", []string{"Manchester City", "Real Madrid", "Liverpool", "Bayern Munich"}, "Manchester City"},
	{"How many Ballon d'Or awards has Cristiano Ronaldo won?", []string{"4", "5", "6", "7"}, "5"},
	{"Which country won the FIFA World Cup in 2010?", []string{"Brazil", "Germany", "Spain", "Netherlands"}, "Spain"},
	{"Who is the manager of Liverpool as of 2023?", []string{"Pep Guardiola", "Erik ten Hag", "Jürgen Klopp", "Carlo Ancelotti"}, "Jürgen Klopp"},
	{"Which player has the most assists in Premier League history?", []string{"David Beckham", "Wayne Rooney", "Thierry Henry", "Ryan Giggs"}, "Ryan Giggs"},
	{"How many times has Juventus won the UEFA Champions League?", []string{"1", "2", "3", "4"}, "2"},
	{"Which player won the 2019 Ballon d'Or?", []string{"Cristiano Ronaldo", "Virgil van Dijk", "Kylian Mbappé", "Neymar"}, "Virgil van Dijk"},
	{"Who was the first African player to win the Ballon d'Or?", []string{"Samuel Eto'o", "George Weah", "Jay-Jay Okocha", "Didier Drogba"}, "George Weah"},
	{"Which team plays at Stamford Bridge?", []string{"Chelsea", "Tottenham", "Arsenal", "West Ham"}, "Chelsea"},
	{"How many goals did Robert Lewandowski score in the 2021-22 season?", []string{"48", "50", "52", "56"}, "50"},
	{"Which player has won the FIFA World Player of the Year award the most times?", []string{"Messi", "Ronaldo", "Maldini", "Zidane"}, "Ronaldo"},
	{"In which year did Barcelona win their first La Liga title?", []string{"1991", "1992", "1993", "1994"}, "1992"},
}

type Game struct {
	p1 *Player
	p2 *Player

	index int
	lock  sync.Mutex
}

// ---------- SEND QUESTION ----------

func (g *Game) sendQuestion() {
	if g.index >= len(questions) {
		g.endGame()
		return
	}

	q := questions[g.index]

	payload := map[string]interface{}{
		"type":           "question",
		"questionIndex":  g.index + 1,
		"q":              q.Q,
		"options":        q.Options,
		"total":          len(questions),
	}

	fmt.Printf("[Game] سوال %d فرستاده شد\n", g.index+1)
	g.sendToPlayers(payload)
}

// ---------- HANDLE ANSWER ----------

func (g *Game) checkAnswer(p *Player, ans string) {
	g.lock.Lock()
	defer g.lock.Unlock()

	if g.index >= len(questions) {
		return
	}

	correct := questions[g.index].Answer
	isCorrect := ans == correct

	if isCorrect {
		p.score += 10
		fmt.Printf("[Game] %s جواب درست: +10 نقطه (کل: %d)\n", p.name, p.score)
		p.conn.WriteJSON(map[string]interface{}{
			"type":  "correct",
			"score": p.score,
		})
	} else {
		p.score -= 10
		fmt.Printf("[Game] %s جواب غلط: -10 نقطه (کل: %d)\n", p.name, p.score)
		p.conn.WriteJSON(map[string]interface{}{
			"type":  "wrong",
			"score": p.score,
		})
	}

	// حریف را اطلاع بده
	opponent := g.p1
	if p == g.p1 {
		opponent = g.p2
	}

	opponent.conn.WriteJSON(map[string]interface{}{
		"type":              "opponent_answered",
		"opponentScore":     p.score,
		"isCorrect":         isCorrect,
	})

	g.index++
	time.Sleep(500 * time.Millisecond)
	g.sendQuestion()
}

// ---------- BROADCAST ----------

func (g *Game) sendToPlayers(data interface{}) {
	g.p1.conn.WriteJSON(data)
	g.p2.conn.WriteJSON(data)
}

// ---------- END GAME ----------

func (g *Game) endGame() {
	winner := g.p1.name
	winnerScore := g.p1.score
	loserScore := g.p2.score

	if g.p2.score > g.p1.score {
		winner = g.p2.name
		winnerScore = g.p2.score
		loserScore = g.p1.score
	}

	fmt.Printf("[Game] بازی تمام: %s (%d) vs (%d)\n", winner, winnerScore, loserScore)

	g.p1.conn.WriteJSON(map[string]interface{}{
		"type":          "end",
		"myScore":       g.p1.score,
		"opponentScore": g.p2.score,
		"myName":        g.p1.name,
		"opponentName":  g.p2.name,
		"winner":        winner,
	})

	g.p2.conn.WriteJSON(map[string]interface{}{
		"type":          "end",
		"myScore":       g.p2.score,
		"opponentScore": g.p1.score,
		"myName":        g.p2.name,
		"opponentName":  g.p1.name,
		"winner":        winner,
	})

	g.p1.conn.Close()
	g.p2.conn.Close()
}

// ---------- LISTEN ----------

func (g *Game) listen(p *Player) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("[Error] %s: %v\n", p.name, r)
		}
	}()

	for {
		var msg map[string]interface{}
		err := p.conn.ReadJSON(&msg)
		if err != nil {
			fmt.Printf("[WebSocket] %s قطع شد\n", p.name)
			return
		}

		msgType, ok := msg["type"].(string)
		if !ok {
			continue
		}

		if msgType == "answer" {
			if ans, ok := msg["answer"].(string); ok {
				fmt.Printf("[WebSocket] %s جواب: %s\n", p.name, ans)
				g.checkAnswer(p, ans)
			}
		}
	}
}

// ---------- MATCHMAKING ----------

var queue []*Player
var mu sync.Mutex

func handler(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		fmt.Printf("[Error] اتصال ناموفق: %v\n", err)
		return
	}

	player := &Player{
		conn:      conn,
		score:     0,
		name:      "Unknown",
		sessionID: fmt.Sprintf("%d", time.Now().UnixNano()),
	}

	fmt.Printf("[WebSocket] کاربر جدید متصل شد\n")

	// دریافت نام بازیکن
	var joinMsg map[string]interface{}
	err = conn.ReadJSON(&joinMsg)
	if err != nil {
		fmt.Printf("[Error] خطا در دریافت نام: %v\n", err)
		conn.Close()
		return
	}

	if name, ok := joinMsg["name"].(string); ok && name != "" {
		player.name = name
		fmt.Printf("[Player] %s به صف پیوست\n", player.name)
	}

	mu.Lock()
	queue = append(queue, player)
	queueLength := len(queue)

	if queueLength >= 2 {
		p1 := queue[0]
		p2 := queue[1]
		queue = queue[2:]

		fmt.Printf("[Matchmaking] بازی: %s vs %s\n", p1.name, p2.name)

		game := &Game{
			p1:    p1,
			p2:    p2,
			index: 0,
		}

		// پیام شروع بازی
		p1.conn.WriteJSON(map[string]interface{}{
			"type":         "match",
			"opponentName": p2.name,
		})

		p2.conn.WriteJSON(map[string]interface{}{
			"type":         "match",
			"opponentName": p1.name,
		})

		game.sendQuestion()

		go game.listen(p1)
		go game.listen(p2)
	} else {
		fmt.Printf("[Matchmaking] منتظر... (صف: %d)\n", queueLength)
		conn.WriteJSON(map[string]interface{}{
			"type":    "waiting",
			"message": "منتظر بازیکن دیگر...",
		})
	}

	mu.Unlock()
}

// ---------- MAIN ----------

func main() {
	http.HandleFunc("/ws", handler)

	fmt.Println("🚀 سرور WebSocket شروع شد - :8081")
	err := http.ListenAndServe(":8081", nil)
	if err != nil {
		fmt.Printf("❌ خطا: %v\n", err)
	}
}
