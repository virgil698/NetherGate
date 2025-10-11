/**
 * NetherGate JavaScript/TypeScript API 类型定义
 * @version 1.0.0
 */

declare module 'nethergate' {
    // ===== 插件信息 =====

    export interface PluginInfo {
        id: string;
        name: string;
        version: string;
        description?: string;
        author?: string;
        website?: string;
    }

    // ===== 日志记录 =====

    export interface Logger {
        trace(message: string): void;
        debug(message: string): void;
        info(message: string): void;
        warning(message: string): void;
        warn(message: string): void;
        error(message: string): void;
    }

    // ===== 事件系统 =====

    export interface EventBus {
        subscribe(eventName: string, handler: (event: any) => void | Promise<void>): void;
        unsubscribe(eventName: string, handler: Function): void;
        publish(eventName: string, eventData?: any): void;
        clearAll(): void;
    }

    export interface ServerStartedEvent {
        timestamp: string;
    }

    export interface PlayerJoinEvent {
        playerName: string;
        playerUuid: string;
        timestamp: string;
    }

    export interface PlayerLeaveEvent {
        playerName: string;
        playerUuid: string;
        timestamp: string;
    }

    // ===== 命令系统 =====

    export interface CommandContext {
        sender: string;
        args: string[];
        reply(message: string): Promise<void>;
    }

    export interface CommandOptions {
        name: string;
        callback: (context: CommandContext) => void | Promise<void>;
        description?: string;
        usage?: string;
        permission?: string;
        aliases?: string[];
    }

    export interface CommandRegistry {
        register(options: CommandOptions): void;
        unregister(name: string): void;
    }

    // ===== RCON 客户端 =====

    export interface RconResult {
        success: boolean;
        response: string;
    }

    export interface RconClient {
        execute(command: string): Promise<RconResult>;
        executeBatch(commands: string[]): Promise<RconResult[]>;
        isConnected(): boolean;
    }

    // ===== 调度器 =====

    export interface Scheduler {
        runDelayed(callback: () => void | Promise<void>, delayMs: number): string;
        runRepeating(callback: () => void | Promise<void>, intervalMs: number, initialDelayMs?: number): string;
        cancel(taskId: string): void;
        cancelAll(): void;
    }

    // ===== 配置管理 =====

    export interface ConfigManager {
        load(filename: string, defaults?: any): Promise<any>;
        get(path: string, defaultValue?: any): any;
        set(path: string, value: any): void;
        save(filename: string): Promise<void>;
        reload(filename: string): Promise<void>;
    }

    // ===== 权限管理 =====

    export interface PermissionManager {
        hasPermission(player: string, permission: string): Promise<boolean>;
        grantPermission(player: string, permission: string): Promise<void>;
        revokePermission(player: string, permission: string): Promise<void>;
        getPlayerPermissions(player: string): Promise<string[]>;
    }

    // ===== 玩家数据 =====

    export interface PlayerDataReader {
        read(playerName: string): Promise<any>;
        exists(playerName: string): Promise<boolean>;
        getOnlinePlayers(): Promise<string[]>;
    }

    // ===== 计分板 =====

    export interface ScoreboardApi {
        createObjective(name: string, criterion: string, displayName: string): Promise<void>;
        removeObjective(name: string): Promise<void>;
        setDisplay(slot: string, objective: string): Promise<void>;
        addScore(objective: string, player: string, points: number): Promise<void>;
        setScore(objective: string, player: string, points: number): Promise<void>;
        getScore(objective: string, player: string): Promise<number>;
        getScores(objective: string): Promise<Record<string, number>>;
    }

    // ===== WebSocket =====

    export interface WebSocketMessage {
        type: string;
        data: any;
        timestamp: string;
    }

    export interface WebSocketServer {
        broadcast(message: WebSocketMessage | any): Promise<void>;
        send(clientId: string, message: WebSocketMessage | any): Promise<void>;
        getConnectedClients(): string[];
        isClientConnected(clientId: string): boolean;
    }

    // ===== 插件接口 =====

    export interface Plugin {
        info: PluginInfo;
        onLoad?(): Promise<void> | void;
        onEnable?(): Promise<void> | void;
        onDisable?(): Promise<void> | void;
        onUnload?(): Promise<void> | void;
    }

    // ===== 工具函数 =====

    export namespace Utils {
        function parsePosition(str: string): { x: number; y: number; z: number } | null;
        function formatTime(ms: number): string;
        function sleep(ms: number): Promise<void>;
    }
}

// ===== 全局对象 =====

/**
 * CommonJS 模块支持
 */
declare const module: {
    exports: any;
};

declare const exports: any;

declare function require(module: string): any;

