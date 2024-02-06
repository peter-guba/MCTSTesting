# Children of the Galaxy AI

Repository for my diploma thesis **Artificial Intelligence for Children of the Galaxy Computer Game**. Charles University, Faculty of Mathematics and Physics.

More information is in the `Thesis` folder where the actual thesis is `Thesis.docx` and documentation describing how to use the software `UserDocumentation.docx`.

## Abstract

Even though artificial intelligence (AI) agents are now able to solve many classical games, in the
field of computer strategy games, the AI opponents still leave much to be desired. In this work we
tackle a problem of combat in strategy video games by adapting existing search approaches:
Portfolio greedy search (PGS) and Monte-Carlo tree search (MCTS). We also introduce an
improved version of MCTS called MCTS considering hit points (MCTS_HP). These methods are
evaluated in context of a recently released 4X strategy game Children of the Galaxy. We
implement a combat simulator for the game and a benchmarking framework where various AI
approaches can be compared. We show that for small to medium combat MCTS methods are
superior to PGS. In all scenarios MCTS_HP is equal or better than regular MCTS due to its better
search guidance. In smaller scenarios MCTS_HP with only 100 millisecond time limit outperforms
regular MCTS with 2 second time limit. By combining fast greedy search for large combats and
more precise MCTS_HP for smaller scenarios a universal AI player can be created.
