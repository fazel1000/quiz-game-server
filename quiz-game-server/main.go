package main

import (
	"encoding/json"
	"fmt"
	"net/http"
	"sync"

	"github.com/gorilla/websocket"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

// ---------- MODELS ----------

type Player struct {
	conn  *websocket.Conn
	score int
}

type Question struct {
	Q       string   `json:"q"`
	Options []string `json:"options"`
	Answer  string   `json:"-"`
}

// ---------- GAME DATA ----------

var questions = []Question{
	{"2 + 2", []string{"3", "4", "5", "6"}, "4"},
	{"5 + 3", []string{"6", "7", "8", "9"}, "8"},
	{"10 - 6", []string{"1", "2", "3", "4"}, "4"},
	{"7 + 1", []string{"6", "7", "8", "9"}, "8"},
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
		"type":    "question",
		"q":       q.Q,
		"options": q.Options,
	}

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

	if ans == correct {
		p.score++
		p.conn.WriteJSON(map[string]interface{}{
			"type":  "correct",
			"score": p.score,
		})
	} else {
		p.score--
		p.conn.WriteJSON(map[string]interface{}{
			"type":  "wrong",
			"score": p.score,
		})
	}

	g.index++
	g.sendQuestion()
}

// ---------- BROADCAST ----------

func (g *Game) sendToPlayers(data interface{}) {
	g.p1.conn.WriteJSON(data)
	g.p2.conn.WriteJSON(data)
}

// ---------- END GAME ----------

func (g *Game) endGame() {
	g.p1.conn.WriteJSON(map[string]interface{}{
		"type":  "end",
		"score": g.p1.score,
	})

	g.p2.conn.WriteJSON(map[string]interface{}{
		"type":  "end",
		"score": g.p2.score,
	})
}

// ---------- LISTEN ----------

func (g *Game) listen(p *Player) {
	for {
		var msg map[string]interface{}
		err := p.conn.ReadJSON(&msg)
		if err != nil {
			return
		}

		if ans, ok := msg["answer"].(string); ok {
			g.checkAnswer(p, ans)
		}
	}
}

// ---------- MATCHMAKING ----------

var queue []*Player
var mu sync.Mutex

func handler(w http.ResponseWriter, r *http.Request) {
	conn, _ := upgrader.Upgrade(w, r, nil)

	player := &Player{conn: conn, score: 0}

	fmt.Println("User connected")

	mu.Lock()
	queue = append(queue, player)

	if len(queue) >= 2 {
		p1 := queue[0]
		p2 := queue[1]
		queue = queue[2:]

		game := &Game{
			p1:    p1,
			p2:    p2,
			index: 0,
		}

		fmt.Println("Match started!")

		game.sendQuestion()

		go game.listen(p1)
		go game.listen(p2)
	}

	mu.Unlock()
}

// ---------- MAIN ----------

func main() {
	http.HandleFunc("/ws", handler)

	fmt.Println("Server started on :8081")
	http.ListenAndServe(":8081", nil)
}