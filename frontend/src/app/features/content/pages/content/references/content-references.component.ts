/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit } from '@angular/core';
import { AppLanguageDto, ComponentContentsState, ContentDto, QuerySynchronizer, Router2State, ToolbarService, TypedSimpleChanges } from '@app/shared';

@Component({
    selector: 'sqx-content-references[content][language][languages]',
    styleUrls: ['./content-references.component.scss'],
    templateUrl: './content-references.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        Router2State, ComponentContentsState,
    ],
})
export class ContentReferencesComponent implements OnInit, OnDestroy {
    @Input()
    public content!: ContentDto;

    @Input()
    public language!: AppLanguageDto;

    @Input()
    public languages!: ReadonlyArray<AppLanguageDto>;

    @Input()
    public mode: 'references' | 'referencing' = 'references';

    constructor(
        public readonly contentsRoute: Router2State,
        public readonly contentsState: ComponentContentsState,
        private readonly changeDetector: ChangeDetectorRef,
        private readonly toolbar: ToolbarService,
    ) {
    }

    public ngOnDestroy() {
        this.toolbar.remove(this);
    }

    public ngOnInit() {
        this.toolbar.addButton(this, 'i18n:contents.validate', () => {
            this.validate();

            this.changeDetector.detectChanges();
        });

        this.toolbar.addButton(this, 'i18n:contents.publishAll', () => {
            this.publishAll();

            this.changeDetector.detectChanges();
        });
    }

    public ngOnChanges(changes: TypedSimpleChanges<this>) {
        if (changes.content || changes.mode) {
            this.contentsState.schema = { name: this.content.schemaName };

            const initial =
                this.contentsRoute.mapTo(this.contentsState)
                    .withPaging('contents', 10)
                    .withSynchronizer(QuerySynchronizer.INSTANCE)
                    .getInitial();

            if (this.mode === 'references') {
                this.contentsState.loadReferences(this.content.id, initial);
            } else {
                this.contentsState.loadReferencing(this.content.id, initial);
            }

            this.contentsRoute.listen();
        }
    }

    private validate() {
        this.contentsState.validate(this.contentsState.snapshot.contents);
    }

    private publishAll() {
        this.contentsState.changeManyStatus(this.contentsState.snapshot.contents.filter(x => x.canPublish), 'Published');
    }

    public trackByContent(_index: number, content: ContentDto) {
        return content.id;
    }
}
