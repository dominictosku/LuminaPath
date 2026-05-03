import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

export type QuestType = 'main' | 'sub' | 'faction';

export type Quest = {
  id: number;
  title: string;
  completed: boolean;
  completedAt?: string;
  createdAt?: string;
  rewardXp?: number;
};

export type QuestSkill = {
  id: number;
  name: string;
  icon: string;
  color: string;
  xp: number;
  nodes: string[];
  unlockedNodes: number[];
};

export type QuestBoardState = {
  xp: number;
  quests: Record<QuestType, Quest[]>;
  skills: QuestSkill[];
};

type ApiQuestType = 0 | 1 | 2;

type ApiQuestBoard = {
  xp: number;
  quests: ApiQuest[];
  skills: ApiQuestSkill[];
};

type ApiQuest = {
  id: number;
  title: string;
  type: ApiQuestType;
  rewardXp: number;
  completed: boolean;
  completedAt?: string;
  createdAt?: string;
  sortOrder: number;
};

type ApiQuestSkill = {
  id: number;
  name: string;
  icon: string;
  color: string;
  xp: number;
  sortOrder: number;
  nodes: ApiQuestSkillNode[];
};

type ApiQuestSkillNode = {
  id: number;
  name: string;
  unlocked: boolean;
  unlockedAt?: string;
  sortOrder: number;
};

@Injectable({
  providedIn: 'root',
})
export class QuestBoardService {
  private readonly httpConfig = { withCredentials: true };

  constructor(private http: HttpClient, private apiEndpoint: ApiEndpointService) {}

  async getBoard(): Promise<QuestBoardState> {
    const board = await firstValueFrom(this.http.get<ApiQuestBoard>(this.apiEndpoint.url('quests/board'), this.httpConfig));
    return this.toState(board);
  }

  async saveBoard(state: QuestBoardState): Promise<QuestBoardState> {
    const board = await firstValueFrom(this.http.put<ApiQuestBoard>(this.apiEndpoint.url('quests/board'), this.toApi(state), this.httpConfig));
    return this.toState(board);
  }

  private toState(board: ApiQuestBoard): QuestBoardState {
    const quests: Record<QuestType, Quest[]> = {
      main: [],
      sub: [],
      faction: [],
    };

    for (const quest of board.quests) {
      quests[this.toQuestType(quest.type)].push({
        id: quest.id,
        title: quest.title,
        completed: quest.completed,
        completedAt: quest.completedAt,
        createdAt: quest.createdAt,
        rewardXp: quest.rewardXp,
      });
    }

    return {
      xp: board.xp,
      quests,
      skills: board.skills.map((skill) => ({
        id: skill.id,
        name: skill.name,
        icon: skill.icon,
        color: skill.color,
        xp: skill.xp,
        nodes: skill.nodes.map((node) => node.name),
        unlockedNodes: skill.nodes
          .map((node, index) => (node.unlocked ? index : -1))
          .filter((index) => index >= 0),
      })),
    };
  }

  private toApi(state: QuestBoardState): ApiQuestBoard {
    return {
      xp: state.xp,
      quests: (['main', 'sub', 'faction'] as QuestType[]).flatMap((type) =>
        state.quests[type].map((quest, index) => ({
          id: quest.id > 0 ? quest.id : 0,
          title: quest.title,
          type: this.toApiQuestType(type),
          rewardXp: quest.rewardXp ?? this.rewardFor(type),
          completed: quest.completed,
          completedAt: quest.completedAt,
          createdAt: quest.createdAt,
          sortOrder: index,
        }))
      ),
      skills: state.skills.map((skill, skillIndex) => ({
        id: skill.id > 0 ? skill.id : 0,
        name: skill.name,
        icon: skill.icon,
        color: skill.color,
        xp: skill.xp,
        sortOrder: skillIndex,
        nodes: skill.nodes.map((node, nodeIndex) => ({
          id: 0,
          name: node,
          unlocked: skill.unlockedNodes.includes(nodeIndex),
          unlockedAt: skill.unlockedNodes.includes(nodeIndex) ? new Date().toISOString() : undefined,
          sortOrder: nodeIndex,
        })),
      })),
    };
  }

  private toQuestType(type: ApiQuestType): QuestType {
    return type === 0 ? 'main' : type === 2 ? 'faction' : 'sub';
  }

  private toApiQuestType(type: QuestType): ApiQuestType {
    return type === 'main' ? 0 : type === 'faction' ? 2 : 1;
  }

  private rewardFor(type: QuestType): number {
    return type === 'main' ? 150 : type === 'faction' ? 100 : 75;
  }
}
