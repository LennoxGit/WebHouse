﻿using System.Net.WebSockets;
using System.Reflection;
using WebHouse_Client.Components;
using WebHouse_Client.Logic;
using WebHouse_Client.Networking;
using ChapterCard = WebHouse_Client.Logic.ChapterCard;

namespace WebHouse_Client;

public partial class GameForm : Form
{
    public static GameForm Instance { get; set; }
    
    private PictureBox? roomImage;
    private PictureBox? bookImage;
    private PictureBox? playerImage;
    private PictureBox? opponentImage;
    private Panel? inventoryContainer;
    public ChapterCard? specialChapterCard;
    public PictureBox? drawChapterCardButton;
    public PictureBox? drawEscapeCardButton;
    private PictureBox? discardPile;
    public List<ChapterCardPile> discardPiles = new List<ChapterCardPile>();
    private Panel? infoPanel;
    private Label? timerLabel;
    private List<Label> playerLabels = new List<Label>();
    
    private Rectangle boardContainer;
    private int widthUnit;
    private int heightUnit;
    
    private bool isFullScreen = false;
    private Rectangle previousBounds;
    
    public bool blockDrawingEscapeCard = false;
    
    public GameForm()
    {
        Instance = this;
        
        InitializeComponent();
        
        this.DoubleBuffered = true;
        BackgroundImage = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("WebHouse_Client.Resources.Background_Images.Wood.jpg"));
        this.BackgroundImageLayout = ImageLayout.Stretch;
        
        //this.FormBorderStyle = FormBorderStyle.None; //kein Rand
        this.WindowState = FormWindowState.Maximized; //macht Vollbild
        this.SizeChanged += (_, _) =>
        {
            RenderBoard();
        };
        this.FormClosing += (s, e) =>
        {
            if (NetworkManager.Instance != null && NetworkManager.Instance.Client != null)
                NetworkManager.Instance.Client.Stop(WebSocketCloseStatus.NormalClosure, "Client closed");
            Application.Exit();
        };
        
        GameLogic.Start();

        RenderBoard();
    }
    
    public void RenderBoard()
    {
        boardContainer = new Rectangle();
        
        var boardWidth = ClientSize.Width;
        var boardHeight = ClientSize.Width * 9 / 16;
        if (boardHeight > ClientSize.Height)
        {
            boardWidth = ClientSize.Height * 16 / 9;
            boardHeight = ClientSize.Height;
        }
        
        // Setze Größe und Position des Boards zentriert im Fenster
        boardContainer.Width = boardWidth;
        boardContainer.Height = boardHeight;
        boardContainer.Location = new Point((ClientSize.Width - boardContainer.Width) / 2, (ClientSize.Height - boardContainer.Height) / 2);
        
        widthUnit = boardContainer.Width / 32;
        heightUnit = boardContainer.Height / 18;
        
        // Buch-Hintergrundbild initialisieren
        if (bookImage == null)
        {
            bookImage = new BufferPictureBox();
            bookImage.Image = Image.FromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("WebHouse_Client.Resources.Background_Images.Book.png"));
            bookImage.SizeMode = PictureBoxSizeMode.Zoom;
            bookImage.BackColor = Color.Transparent;
            Controls.Add(bookImage);
        }
        
        // Größe und Position des Buchs anpassen
        bookImage.Width = 16 * widthUnit;//Math.Min(9 * heightUnit, 20 * widthUnit * 9 / 16);//heightUnit * 9;//roomImageHeight;
        bookImage.Height = 9 * heightUnit;
        bookImage.Location = new Point(boardContainer.X + 15 * widthUnit, boardContainer.Y + heightUnit);
        
        if (roomImage == null)
        {
            roomImage = new BufferPictureBox();
            roomImage.BorderStyle = BorderStyle.FixedSingle;
            roomImage.SizeMode = PictureBoxSizeMode.Zoom;
            Controls.Add(roomImage);
        }

        // Bild immer neu setzen
        roomImage.Image = Image.FromStream(
            Assembly.GetExecutingAssembly().GetManifestResourceStream("WebHouse_Client.Resources.Background_Images."+ GameLogic.CurrentRoom.Picture));

        roomImage.Width = (int)Math.Ceiling(bookImage.Width - (bookImage.Width / 1728f) * (85f + 92f));
        roomImage.Height = (int)Math.Ceiling(bookImage.Height - (bookImage.Width / 1728f) * (34f + 64f));
        //Oben rechts positionieren
        roomImage.Location = new Point((int)Math.Ceiling(bookImage.Location.X + bookImage.Width / 1728f * 85f),
            (int)Math.Ceiling(bookImage.Location.Y + bookImage.Width / 1728f * 34f));//new Point(boardContainer.X + boardContainer.Width - widthUnit - roomImage.Width, boardContainer.Y + heightUnit);
        roomImage.BringToFront();
        
        //Inventory Container erstellen (Für Handkarten)
        if (inventoryContainer == null)
        {
            inventoryContainer = new BufferPanel();
            inventoryContainer.BorderStyle = BorderStyle.FixedSingle;
            inventoryContainer.BackColor = Color.FromArgb(50, Color.AntiqueWhite);
            Controls.Add(inventoryContainer);
        }
        
        //setzt die Größe und Position des Inventory Containers
        inventoryContainer.Size = new Size(16 * widthUnit, 6 * heightUnit);//(GetRelativeSize(ClientSize, true, percentage: 50), GetRelativeSize(ClientSize, false, percentage: 33.34));
        inventoryContainer.Location = new Point(boardContainer.X + 15 * widthUnit, boardContainer.Y + 11 * heightUnit);//new Point(boardContainer.X + boardContainer.Width - widthUnit - inventoryContainer.Width, boardContainer.Y + 11 * heightUnit);

        //Berechnet die Größe der Karten im Inventory Container
        var cardHeight = Math.Min(inventoryContainer.Height, GetRelativeSize(inventoryContainer.Size, true, percentage: 100f /
            (GameLogic.MaxCards + 1f)) * 3 / 2);//cardWidth * 3 / 2;
        var cardWidth = cardHeight * 2 / 3;//GetRelativeSize(inventoryContainer.Size, true, percentage: 16.67);
        for (var i = 0; i < GameLogic.Inventory.Count; i++)
        {
            var card = GameLogic.Inventory[i];
            var location = new Point(inventoryContainer.Location.X + (cardWidth / (GameLogic.MaxCards + 1)) 
                + i * cardWidth + i * (cardWidth / (GameLogic.MaxCards + 1)),
                inventoryContainer.Location.Y + (inventoryContainer.Height - cardHeight) / 2);
            var size = new Size(cardWidth, cardHeight);
            
            if (card.Component == null)
            {
                card.CreateComponent();
                Controls.Add(card.Component.Panel);
            }
            card.Component.Panel.Size = size;
            card.Component.Panel.Location = location;
            card.Component.Panel.BringToFront();
        }

        //Spielfigur initialisieren
        if (playerImage == null)
        {
            // Neue PictureBox erzeugen
            playerImage = new BufferPictureBox();
            playerImage.BackColor = Color.Transparent;
            playerImage.SizeMode = PictureBoxSizeMode.Zoom;
            playerImage.Image = Image.FromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream
                    ("WebHouse_Client.Resources.Images.Figure.png"));
            playerImage.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            roomImage.Controls.Add(playerImage);
        }
        
        playerImage.Size = new Size(GetRelativeSize(roomImage.Size, true, 80), GetRelativeSize(roomImage.Size, false, 120));
        
        //Gegner Figur initialisieren
        if (opponentImage == null)
        {
            opponentImage = new BufferPictureBox();
            opponentImage.BackColor = Color.Transparent;
            opponentImage.SizeMode = PictureBoxSizeMode.Zoom;
            opponentImage.Image = Image.FromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream
                    ("WebHouse_Client.Resources.Images.Opponent.png"));
            opponentImage.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            roomImage.Controls.Add(opponentImage);
        }
        
        opponentImage.Size = new Size(GetRelativeSize(roomImage.Size, true, 80), GetRelativeSize(roomImage.Size, false, 120));

        //Positioniere Spieler- und Gegnerfigur im Raum
        UpdatePositions();
        
        // Spezialkarte im Raum darstellen
        if (specialChapterCard == null)
        {
            specialChapterCard = GameLogic.CurrentRoom.SpecialCard;
        }
        
        if (specialChapterCard.Component == null)
        {
            specialChapterCard.CreateComponent();
            Controls.Add(specialChapterCard.Component.Panel);
        }

        specialChapterCard.Component.Panel.Size = new Size(widthUnit * 2, heightUnit * 3);
        specialChapterCard.Component.Panel.Location = new Point(boardContainer.X + 12 * widthUnit, boardContainer.Y + 1 * heightUnit);
        
        //button zum Ziehen von Karten erstellen
        if (drawChapterCardButton == null)
        {
            drawChapterCardButton = new BufferPictureBox();
            drawChapterCardButton.MouseClick += (_, args) =>
            {
                if (args.Button != MouseButtons.Left || GameLogic.Inventory.Count >= GameLogic.MaxCards)
                    return;
                
                if (GameLogic.TurnState == 1)
                    GameLogic.SwitchTurnState();
                
                NetworkManager.Rpc.RequestChapterCard();
                
                RenderBoard();
            };
            drawChapterCardButton.BackColor = Color.Transparent;
            drawChapterCardButton.Image = Image.FromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("WebHouse_Client.Resources.Background_Images.DrawChapterCard.png"));
            drawChapterCardButton.SizeMode = PictureBoxSizeMode.StretchImage;
            drawChapterCardButton.Visible = false;
            Controls.Add(drawChapterCardButton);
        }

        drawChapterCardButton.Size = new Size(widthUnit * 2, heightUnit * 3);
        drawChapterCardButton.Location = new Point(boardContainer.X + 12 * widthUnit, boardContainer.Y + 6 * heightUnit);
        
        //button zum Ziehen von EscapeCards erstellen
        if (drawEscapeCardButton == null)
        {
            drawEscapeCardButton = new BufferPictureBox();
            drawEscapeCardButton.MouseClick += (_, args) =>
            {
                if (args.Button != MouseButtons.Left || GameLogic.Inventory.Count >= GameLogic.MaxCards || blockDrawingEscapeCard)
                    return;

                if (GameLogic.TurnState == 1)
                    GameLogic.SwitchTurnState();
                
                NetworkManager.Rpc.RequestEscapeCard();
                
                RenderBoard();
            };
            drawEscapeCardButton.BackColor = Color.Transparent;
            drawEscapeCardButton.Image = Image.FromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("WebHouse_Client.Resources.Background_Images.DrawEscapeCard.png"));
            drawEscapeCardButton.SizeMode = PictureBoxSizeMode.StretchImage;
            drawEscapeCardButton.Visible = false;
            Controls.Add(drawEscapeCardButton);
        }

        drawEscapeCardButton.Size = new Size(widthUnit * 2, heightUnit * 3);
        drawEscapeCardButton.Location = new Point(boardContainer.X + 12 * widthUnit, boardContainer.Y + 10 * heightUnit);
        
        //Ablagestapel für Karten erstellen
        if (discardPile == null)
        {
            discardPile = new BufferPictureBox();
            discardPile.MouseClick += (_, args) =>
            {
                if (args.Button != MouseButtons.Left)
                    return;
                
                Components.DiscardPile.Disposing();
                RenderBoard();
            };
            discardPile.BackColor = Color.Transparent;
            discardPile.Image = Image.FromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("WebHouse_Client.Resources.Background_Images.DiscardPileWithText.png"));
            discardPile.SizeMode = PictureBoxSizeMode.StretchImage;
            Controls.Add(discardPile);
        }

        discardPile.Size = new Size(widthUnit * 2, heightUnit * 3);
        discardPile.Location = new Point(boardContainer.X + 12 * widthUnit, boardContainer.Y + 14 * heightUnit);
        
        //InfoPanel erstellen, das den Timer und die Spieler anzeigt
        if (infoPanel == null)
        {
            infoPanel = new BufferPanel();
            infoPanel.BorderStyle = BorderStyle.FixedSingle;
            infoPanel.BackColor = Color.FromArgb(100, Color.DimGray);
            Controls.Add(infoPanel);
        }

        infoPanel.Size = new Size(widthUnit * 10, heightUnit * 4);
        infoPanel.Location = new Point(boardContainer.X + 1 * widthUnit, boardContainer.Y + 1 * heightUnit);
        
        var ratioSize = infoPanel.Size;
        
        //Timerlabel erstellen, das die verbleibende Zeit anzeigt
        if (timerLabel == null)
        {
            timerLabel = new Label()
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                UseCompatibleTextRendering = true,
                Font = new Font(Program.Font, Math.Max(12, (int)(ratioSize.Height * 0.15)), FontStyle.Bold, GraphicsUnit.Pixel)
            };
            infoPanel.Controls.Add(timerLabel); 
        }

        timerLabel.Font = new Font(Program.Font, Math.Max(12, (int)(ratioSize.Height * 0.07)), FontStyle.Bold,
            GraphicsUnit.Pixel);
        timerLabel.BringToFront();
        UpdateTimerLabel(GameLogic.PlayTime);
        
        //Spielerlabels initialisieren und anzeigen
        if (NetworkManager.Instance != null)
        {
            if (playerLabels.Count == 0)
            {
                for (var i = 0; i < NetworkManager.Instance.Players.Count; i++)
                {
                    Label lbl = new Label();
                    lbl.Tag = i;
                    lbl.BackColor = Color.Transparent;
                    lbl.AutoSize = true;
                    infoPanel.Controls.Add(lbl);
                    playerLabels.Add(lbl);
                }
            }

            int playerY = timerLabel.Height + infoPanel.Height / 8;
            foreach (var lbl in playerLabels)
            {
                var player = NetworkManager.Instance.Players[lbl.Tag as int? ?? 0];
                lbl.Text = $"{(player.IsHost ? "[Host] " : "")}{player.Name}{(player.Id == NetworkManager.Instance.Id ? " [DU] " : "")}";
                lbl.Font = player.IsTurn 
                    ? new Font("Arial", 14, FontStyle.Bold) 
                    : new Font("Arial", 14, FontStyle.Regular);
                lbl.ForeColor = player.IsTurn ? Color.LightGreen : Color.White;
                lbl.Location = new Point(10, playerY);
                lbl.BringToFront();
                
                playerY += (int)(lbl.Height * 1.2);
            }
        }
        
        //Ablagestapel für ChapterCards erstellen
        if (discardPiles.Count == 0)
        {
            for (int i = 0; i < 9; i++)
            {
                var pile = new ChapterCardPile(i);
                discardPiles.Add(pile);
                Controls.Add(pile.Panel);
            }
        }

        //Positioniere die Ablagestapel für ChapterCards
        foreach (var pile in discardPiles)
        {
            pile.Panel.Size = new Size(2 * widthUnit, 3 * heightUnit);
            pile.Panel.Location = new Point(boardContainer.X + (2 + discardPiles.IndexOf(pile) % 3 * 3) * widthUnit, boardContainer.Y + (6 + discardPiles.IndexOf(pile) / 3 * 4) * heightUnit);
            pile.Panel.BringToFront();
        }

        //Positioniere die ChapterCards auf dem Spielfeld
        foreach (var card in GameLogic.PlacedChapterCards)
        {
            card.Component.Panel.Size = new Size(2 * widthUnit, 3 * heightUnit);
            card.Component.Panel.Location = new Point(boardContainer.X + (2 + discardPiles.IndexOf(((Components.ChapterCard)card.Component).Pile) % 3 * 3) * widthUnit, boardContainer.Y + (6 + discardPiles.IndexOf(((Components.ChapterCard)card.Component).Pile) / 3 * 4) * heightUnit);
            card.Component.Panel.BringToFront();
        }
    }
    
    public void UpdatePositions()
    {
        //Hole die Spielfelder für den aktuellen Raumtyp aus dem Dictionary "Fields"
        var fields = Fields[GameLogic.CurrentRoom.RoomType];
        
        //Spielerposition aktualisieren, wenn sie gültig ist
        if (GameLogic.PlayerPosition >= 0 && GameLogic.PlayerPosition < fields.Count)
        {
            //Hole den Punkt (Koordinaten) aus der Liste der Felder
            var point = fields[GameLogic.PlayerPosition];
            
            //Setze die Position des Spielerbildes relativ zur Raumgrafik
            playerImage.Location = new Point(
                point.X * roomImage.Width / 1920,
                (point.Y - 50) * roomImage.Width / 1920
            );
        }
        
        //Gegnerposition aktualisieren, wenn sie gültig ist
        if (GameLogic.OpponentPosition >= 0 && GameLogic.OpponentPosition < fields.Count)
        {
            var point = fields[GameLogic.OpponentPosition];
            opponentImage.Location = new Point(
                point.X * roomImage.Width / 1920,
                (point.Y - 50) * roomImage.Width / 1920
            );
        }
    }

    //Aktualisiert das Label für den Timer für die Zeitanzeige im InfoPanel
    public void UpdateTimerLabel(int playTime)
    {
        if (playTime > 0)
        {
            timerLabel.Text = $"Noch {playTime} Minuten bis der Verfolger euch eingeholt hat!";
            timerLabel.ForeColor = Color.Red;
        }
        else
        {
            timerLabel.Text = "Der Verfolger hat euch erwischt!"; //Wenn der Timer abgelaufen ist wird der Text geändert
        }
    }
    
    
    private int GetRelativeSize(Size size, bool width, int? pixels = null, double? percentage = null)
    {
        // Wenn ein Pixelwert übergeben wurde:
        if (pixels != null)
        {
            // Skaliere den Pixelwert relativ zur aktuellen Breite oder Höhe
            // (1920x1080 ist die Referenzauflösung)
            return pixels.Value * (width ? size.Width : size.Height) / (width ? 1920 : 1080);
        }
        
        // Wenn ein Prozentwert übergeben wurde:
        if (percentage != null)
        {
            // Berechne den Prozentwert der aktuellen Breite oder Höhe
            return (int) Math.Round((width ? size.Width : size.Height) * percentage.Value / 100f, MidpointRounding.AwayFromZero);
        }
        // Falls weder Pixel noch Prozent angegeben sind: Fehler
        throw new ArgumentException("Either pixels or percentage must be provided.");
    }

    //macht Vollbild wenn F11 gedrückt wird
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F11)
        {
            if (!isFullScreen)
            {
                previousBounds = Bounds;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
                Bounds = previousBounds;
            }

            isFullScreen = !isFullScreen;
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }


    //Das und folgende bis ende des codes sind die koordinaten der Felder in den jeweiligen Räumen für den Verfolger und Spieler
    private static Dictionary<Room.RoomName, List<Point>> Fields = new Dictionary<Room.RoomName, List<Point>>()
    {
        { Room.RoomName.Hotelzimmer, new ()
        {
            new (95, 115),
            new (240, 115),
            new (370, 190),
            new (405, 305),
            new (355, 420),
            new (280, 540),
            new (230, 665),
            new (245, 795),
            new (385, 820),
            new (530, 850),
            new (675, 855),
            new (790, 750),
            new (1055, 775),
            new (1205, 780),
            new (1345, 855),
            new (1460, 765),
            new (1465, 630),
            new (1520, 495),
            new (1595, 370),
            new (1675, 250),
            new (1750, 135),
        } },
        { Room.RoomName.Hafen, new ()
        {
            new (65, 155),
            new (195, 210),
            new (310, 280),
            new (310, 400),
            new (310, 515),
            new (370, 620),
            new (495, 645),
            new (550, 760),
            new (680, 800),
            new (815, 830),
            new (1180, 810),
            new (1290, 720),
            new (1410, 680),
            new (1490, 580),
            new (1490, 470),
            new (1490, 360),
            new (1490, 250),
            new (1535, 155),
            new (1670, 140),
            new (1800, 130)
        } },
        { Room.RoomName.Stadt, new ()
        {
            new (50, 55),
            new (175, 100),
            new (220, 210),
            new (270, 330),
            new (285, 455),
            new (270, 575),
            new (355, 705),
            new (405, 825),
            new (505, 930),
            new (640, 950),
            new (765, 905),
            new (1040, 910),
            new (1170, 930),
            new (1285, 855),
            new (1395, 755),
            new (1420, 640),
            new (1550, 630),
            new (1675, 620),
            new (1785, 540)
            
        } },
        { Room.RoomName.Wald, new ()
        {
            new (50, 720),
            new (160, 695),
            new (260, 615),
            new (335, 520),
            new (450, 485),
            new (570, 470),
            new (685, 490),
            new (800, 505),
            new (1020, 540),
            new (1130, 585),
            new (1240, 630),
            new (1355, 620),
            new (1470, 580),
            new (1580, 510),
            new (1630, 410),
            new (1670, 310),
            new (1720, 215),
            new (1800, 120),
        } },
        { Room.RoomName.Safehouse, new ()
        {
            new (20, 25),
            new (130, 40),
            new (160, 140),
            new (275, 155),
            new (390, 195),
            new (500, 180),
            new (580, 85),
            new (690, 60),
            new (810, 25),
            new (1035, 25),
            new (1105, 115),
            new (1220, 135),
            new (1330, 125),
            new (1440, 175),
            new (1390, 270),
            new (1360, 370),
            new (1360, 500),
            new (110, 765),
            new (245, 805),
            new (385, 825),
            new (520, 855),
            new (665, 905),
            new (795, 880),
            new (1040, 880),
            new (1160, 875),
            new (1280, 835),
            new (1395, 755),
            new (1360, 645),
            new (1365, 510),
        } },
    };
}