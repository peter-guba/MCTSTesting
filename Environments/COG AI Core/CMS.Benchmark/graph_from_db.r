if (!require("ggplot2")) {
  install.packages("ggplot2")
}
if (!require("reshape2")) {
  install.packages("reshape2")
}
library("ggplot2")
library("reshape2")

makeWinRateGraph <- function(yLabel, d, yData, title_count, titleAppendix, sym=FALSE) {
  title <- "wins"
  if (yLabel == "symmetric") {
    title <- "sym-wins"
  }
  ggplot(data = d, aes(
    x = d$name,
    y = yData$estimate,
    ymin = yData$lower,
    ymax = yData$upper,
    fill = name,
    label = sprintf("%0.2f", round(yData$estimate, digits = 2)))) +
    geom_bar(colour="black", size=.1, stat="identity", position=position_dodge()) +
    geom_errorbar(size = 0.5, width = 0.7, alpha = 0.8) +
    scale_y_continuous(limits = c(0, 1)) +
    geom_text(size = 8, hjust = 0.5, vjust=-0.1) +
    guides(fill=FALSE) +
    labs(x="AI", y=paste(yLabel, "winrate", sep=" ")) +
    ggtitle(paste("Round robin", titleAppendix, title_count, "vs", title_count, title, sep=" ")) +
    theme(
      plot.title = element_text(hjust = 0.5),
      text = element_text(size = 20, colour="black"),
      axis.text.x = element_text(angle=30, hjust=1)
    )
}

makeDouble <- function(yLabel, d, title_count, titleAppendix, sym=FALSE) {
  title <- "wins"
  if (yLabel == "symmetric") {
    title <- "sym-wins"
  }
  ggplot(data = d, aes(
    x = as.factor(name),
    y = est,
    ymin = low,
    ymax = up,
    #fill = interaction(name, type),
    #fill = name,
    fill = type,
    group = type,
    #alpha=type,
    #linetype = name,
    label = sprintf("%0.2f", round(est, digits = 2)))
  ) +
    geom_bar(colour="black", size=.1, stat="identity", position=position_dodge(0.9)) +
    geom_errorbar(size = 0.3, width = 0.7, alpha = 0.8, position=position_dodge(0.9)) +
    scale_y_continuous(limits = c(0, 1)) +
    geom_text(size = 7, hjust = 0.5, vjust=-0.1, position=position_dodge(0.9)) +
    #scale_alpha_manual(values=c(1, 0.7)) +
    #guides(fill=FALSE) +
    labs(x="AI", y=paste(yLabel, "winrate", sep=" ")) +
    ggtitle(paste("Round robin", titleAppendix, title_count, "vs", title_count, title, sep=" ")) +
    theme(
      plot.title = element_text(hjust = 0.5),
      text = element_text(size = 20, colour="black"),
      axis.text.x = element_text(angle=30, hjust=1),
      legend.position=c(0.1, 0.9),
      legend.title = element_blank()
    )
}

makeHPGraph <- function(d, title_count, titleAppendix) {
  title <- "HP remaining"
  ggplot(data = d, aes(
    x = d$name,
    y = d$hull,
    fill = name,
    label = sprintf("%d", d$hull))) +
    geom_bar(colour="black", size=.1, stat="identity", position=position_dodge()) +
    #scale_y_continuous(limits = c(0, 1)) +
    geom_text(size = 7, hjust = 0.5, vjust=-0.1) +
    guides(fill=FALSE) +
    labs(x="AI", y="HP remaining") +
    ggtitle(paste("Round robin", titleAppendix, title_count, "vs", title_count, title, sep=" ")) +
    theme(
      plot.title = element_text(hjust = 0.5),
      text = element_text(size = 20, colour="black"),
      axis.text.x = element_text(angle=30, hjust=1)
    )
}

counts <- c("3", "5", "7", "9", "16", "32", "48", "64", "all", "5to9", "16to32", "48to64", "48_no_limit")
#counts <- c(3)

titles <- counts
titles[length(titles) - 3] = "[5, 7, 9]"
titles[length(titles) - 2] = "[16, 32]"
titles[length(titles) - 1] = "[48, 64]"
titles[length(titles)] = "48"

#counts <- c("5to9")
#titles <- c("[5, 7, 9]")
#counts <- c("48_no_limit")
#titles <- c("48")

directory <- "d:/tmp/cog/results/"

i <- 1
for (count in counts) {
  titleAppendix <- ""
  if (count == "48_no_limit") {
    titleAppendix <- "PGS no limit"
  }
  dataFile = paste(directory, 'round_robin_', count, '.csv', sep="")
  
  outDir = substr(dataFile, 1, nchar(dataFile) - 4)
  if (!dir.exists(outDir)) {
    dir.create(outDir)
  }
  
  fileName <- sub(".csv", "", tail(unlist(strsplit(dataFile, "/")), n = 1))
  
  d <- read.csv(dataFile, sep = ",", stringsAsFactors = FALSE)
  
  countStats <- function(propTestData) {
    estim <- t(t(sapply(Map(function (x) { x[["estimate"]] }, propTestData), "[[", "p")))
    colnames(estim) <- "estimate"
    bounds <- t(sapply(propTestData, "[[","conf.int"))
    colnames(bounds) <- c("lower", "upper")
    return(data.frame(estim, bounds))
  }
  
  d$win_rate <- countStats(Map(prop.test, x = d$win, n = (2 * d$iterations)))
  d$sym_win_rate <- countStats(Map(prop.test, x = d$sym_win, n = d$iterations))
  
  d$win_rate_e <- d$win_rate$estimate
  d$win_rate_l <- d$win_rate$lower
  d$win_rate_u <- d$win_rate$upper
  
  d$name <- factor(d$name, levels=c("Kiter", "NOKAV", 
                                    "PGS_1_0", "PGS_3_3", "PGS_5_5", 
                                    "MCTS_100", "MCTS_500", "MCTS_2000",
                                    "MCTS_HP_100", "MCTS_HP_500", "MCTS_HP_2000"))
  
  df <- data.frame(name=character(), type=character(), est=double(), low=double(), up=double(),stringsAsFactors=FALSE)
  
  for(n in d[, 'name']) {
      row <- subset(d, name == n)
      df[nrow(df) + 1,] = list(n, 'win', row$win_rate$estimate, row$win_rate$lower, row$win_rate$upper)
      df[nrow(df) + 1,] = list(n, 'symwin', row$sym_win_rate$estimate, row$sym_win_rate$lower, row$sym_win_rate$upper)
  }
  
  df$name <- factor(df$name, levels=c("Kiter", "NOKAV", 
                                      "PGS_1_0", "PGS_3_3", "PGS_5_5", 
                                      "MCTS_100", "MCTS_500", "MCTS_2000",
                                      "MCTS_HP_100", "MCTS_HP_500", "MCTS_HP_2000"))
  
  p1 <- makeWinRateGraph("", d, d$win_rate, titles[i], titleAppendix)
  ggsave(paste(outDir, paste("win_rates_",count,".png", sep=""), sep="/"), width=14, height=7)
  p2 <- makeWinRateGraph("symmetric", d, d$sym_win_rate, titles[i], titleAppendix)
  ggsave(paste(outDir, paste("sym_win_rates_",count,".png", sep=""), sep="/"), width=14, height=7)
  p_hp <- makeHPGraph(d, titles[i], titleAppendix)
  ggsave(paste(outDir, paste("hp_remaining_",count,".png", sep=""), sep="/"), width=14, height=7)
  p_stacked <- makeDouble("", df, titles[i], titleAppendix)
  ggsave(paste(outDir, paste("wins_stacked_",count,".png", sep=""), sep="/"), width=14, height=7)
  
  i = i + 1
}
